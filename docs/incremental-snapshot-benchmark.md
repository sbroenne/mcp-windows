# Incremental UI snapshot benchmark

Incremental snapshots exist to reduce the repeated UI context sent to an agent. A complete
accessibility tree is still captured on every request, but `mode=auto` compares it with the
remembered tree and returns `kind=diff` only when the change list is safe and less than 80% of the
serialized full response. Otherwise it returns `kind=full`. This preserves correctness while making
payload savings workload-dependent rather than guaranteed.

The full response contains the documented compact `tree` and no longer serializes the former
redundant full-detail `elements` copy. Consumers that relied on that duplicate should migrate to
`tree`, or call `ui_find` for a flat result.

## Result

Five representative workflows were run five times per comparison arm on Windows
`10.0.26220.0`. The browser pages were the public `microsoft/vscode` GitHub repository, not a
synthetic TodoMVC page.

| Workload | Environment | Full bytes | Auto bytes | Byte savings | Full tokens | Auto tokens | Token savings | Auto full/diff |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Electron form navigation | Electron 44.0.0 | 64,261 | 3,731 | 94.2% | 19,104 | 1,095 | 94.3% | 0/20 |
| GitHub repository navigation | Edge 152.0.4191.41 | 170,352 | 170,461 | n/a | 53,485 | 53,512 | n/a | 20/0 |
| GitHub repository navigation | Chrome 151.0.7922.174 | 66,281 | 63,665 | 3.9% | 20,682 | 19,932 | 3.6% | 17/3 |
| Word document editing | Word 16.0.20326.20100 | 9,200 | 1,440 | 84.3% | 2,848 | 396 | 86.1% | 0/20 |
| Excel worksheet editing | Excel 16.0.20326.20100 | 13,248 | 1,352 | 89.8% | 4,048 | 376 | 90.7% | 0/20 |
| **Mode-effect aggregate** | | **323,342** | **240,540** | **25.6%** | **100,167** | **75,284** | **24.8%** | **37/63** |

Bytes and tokens are medians of the total payload for four post-action snapshots in one run. The
mode-effect aggregate sums the five full medians and substitutes the corresponding full median for
an automatic arm that returned no diffs. This assigns zero incremental savings to full-only
workloads instead of misclassifying live-site variance as a mode effect. Because every workload has
four observations, each workflow has equal weight. Tokens are a SharpToken `cl100k_base`
approximation, not universal model billing tokens.

The measured benefit is strong but not universal. Electron, Word, and Excel produced a diff after
every action, reducing median payloads by 84-94%. Edge used the safe full-response fallback after all
20 live GitHub navigations. Chrome returned three diffs and 17 full responses, reducing its median
payload by 3.9% by bytes and 3.6% by approximate tokens. Across this deliberately mixed workload set,
the normalized mode effect was 25.6% by bytes and 24.8% by approximate tokens. A separate regression
confirms that a same-page GitHub search-field edit returns a scoped diff in both Edge and Chrome; it
is not included as another benchmark workload.

## Chromium noise spike

We tested whether Windows UI Automation's smaller "content view" could remove Chromium layout noise
before snapshots were compared. The same GitHub navigation workflow was run five times per arm with
that view enabled.

| Browser | Full bytes | Auto bytes | Full tokens | Auto tokens | Auto full/diff |
|---|---:|---:|---:|---:|---:|
| Edge | 78,233 | 78,618 | 24,366 | 24,485 | 20/0 |
| Chrome | 162,258 | 162,349 | 51,213 | 51,244 | 20/0 |

This did not produce a single diff. More importantly, a safety check found that the content-view
tree contained the browser frame but omitted GitHub's page controls, including the Code link, in
both Edge and Chrome. Chrome's median full payload was about 6% smaller than the original control
view, but the missing page made that reduction unusable. The experiment was therefore rejected and
is not enabled in production. Edge's live tree differed too much between runs to make a reliable
size comparison.

Playwright's [ARIA snapshot implementation][playwright-aria] supports a safer direction. It creates
a small role, name, text, and state tree rather than comparing raw browser nodes. Its
[distiller][playwright-distiller] joins adjacent text, normalizes whitespace, removes empty text, and
unwraps low-information layout containers with one child. Action references are handled separately
and are renewed after navigation. Playwright's loose role-and-name matching is suitable for test
assertions, but not for carrying an action ID across duplicate controls.

We therefore tested post-capture cleanup rather than Windows content-view filtering. The experiment
unwrapped only unnamed one-child `Pane` and `Group` containers when the
wrapper and child had the same bounds, visibility, and enabled state, the wrapper had no developer
ID, and Windows reported no supported action pattern. It preserved GitHub's Code and Issues controls
in both browsers, but still produced 40 complete responses and no diffs. Checking action patterns
also added provider calls to each candidate wrapper. The cleanup was rejected because it added work
without improving incremental responses.

This experiment also exposed a benchmark problem. Chromium's recommended depth of 15 reached the
browser frame but not GitHub's page controls. The browser scenarios now explicitly use depth 20 and
wait until a real page control appears before each measured snapshot. An integration test verifies
that GitHub's Code control is present. The browser and aggregate results in this document are the
corrected page-content measurements.

[playwright-aria]: https://github.com/microsoft/playwright/blob/32095eac6a944a6d9eb38198f68a4cee9562b3b9/packages/injected/src/ariaSnapshot.ts
[playwright-distiller]: https://github.com/microsoft/playwright/blob/32095eac6a944a6d9eb38198f68a4cee9562b3b9/packages/injected/src/ariaSnapshotDistiller.ts

## Method

Each scenario executes the same four state changes under three arms:

1. **Action-only control** performs the actions without a snapshot. It is a latency floor only and
   is never the payload-savings denominator.
2. **Full baseline** requests `mode=full` after every action.
3. **Automatic treatment** establishes an unmeasured baseline with `mode=reset`, then requests
   `mode=auto` after every action. Production logic decides whether each response is a diff or a
   safe full fallback.

Every arm starts from equivalent application content. Browser and Office scenarios launch isolated
processes and temporary state; Electron resets its dedicated test harness. Arm order rotates between
samples to limit warm-cache sequence bias. Measurements serialize the actual `UIAutomationResult`
with the production JSON options. Action and snapshot elapsed time are recorded separately.

The workflows are:

- **Electron:** navigate the real Electron harness through Forms, Data, Settings, and Home.
- **Edge and Chrome:** navigate the public `microsoft/vscode` GitHub repository through Issues,
  Pull requests, Actions, and Code in isolated browser profiles. These scenarios use depth 20 so the
  snapshot includes the webpage, not only the browser frame.
- **Word:** edit, append to, undo in, and edit a dedicated temporary RTF document.
- **Excel:** enter four values into a dedicated temporary CSV workbook.

| Workload | Action-only ms | Full snapshot ms | Auto snapshot ms |
|---|---:|---:|---:|
| Electron | 928.1 | 5,037.4 | 5,023.0 |
| Edge | 5,394.6 | 24,571.8 | 24,205.0 |
| Chrome | 5,935.4 | 8,242.2 | 9,541.4 |
| Word | 16.9 | 1,797.3 | 1,692.8 |
| Excel | 6.8 | 2,530.2 | 2,440.3 |

These are median totals for four actions or snapshots, not per-call values. Automatic mode still
captures a complete accessibility tree before comparing it, so it is designed to reduce response
payload and agent context, not capture time.

## Raw samples

Each row is one complete four-action run. `Full/diff` counts the response kinds returned during
that run.

### Electron

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 1,120.9 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 924.4 | 5,037.4 | 64,261 | 19,104 | 4/0 |
| 1 | auto | 1,003.2 | 5,151.4 | 3,731 | 1,095 | 0/4 |
| 2 | full | 911.4 | 4,872.9 | 64,261 | 19,104 | 4/0 |
| 2 | auto | 890.3 | 5,023.0 | 3,731 | 1,095 | 0/4 |
| 2 | action-only | 928.1 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 905.3 | 4,857.5 | 3,731 | 1,095 | 0/4 |
| 3 | action-only | 887.3 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 950.8 | 5,068.4 | 64,261 | 19,104 | 4/0 |
| 4 | action-only | 888.7 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 903.2 | 4,825.8 | 64,261 | 19,104 | 4/0 |
| 4 | auto | 914.1 | 4,903.8 | 3,731 | 1,095 | 0/4 |
| 5 | full | 910.1 | 5,053.8 | 64,261 | 19,104 | 4/0 |
| 5 | auto | 1,059.2 | 5,571.8 | 3,731 | 1,095 | 0/4 |
| 5 | action-only | 944.1 | 0.0 | 0 | 0 | 0/0 |

### Edge

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 8,127.6 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 5,217.3 | 24,571.8 | 168,713 | 52,268 | 4/0 |
| 1 | auto | 7,012.7 | 25,134.7 | 170,282 | 53,458 | 4/0 |
| 2 | full | 7,194.6 | 25,931.7 | 170,183 | 53,426 | 4/0 |
| 2 | auto | 5,720.9 | 23,570.3 | 170,378 | 53,488 | 4/0 |
| 2 | action-only | 4,894.2 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 6,473.2 | 23,322.0 | 170,461 | 53,512 | 4/0 |
| 3 | action-only | 6,283.5 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 5,137.3 | 24,305.6 | 170,352 | 53,485 | 4/0 |
| 4 | action-only | 5,394.6 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 6,478.9 | 24,709.7 | 171,439 | 53,488 | 4/0 |
| 4 | auto | 8,114.6 | 24,205.0 | 172,305 | 53,571 | 4/0 |
| 5 | full | 6,691.4 | 24,102.3 | 172,109 | 53,512 | 4/0 |
| 5 | auto | 6,564.4 | 24,337.1 | 172,503 | 53,632 | 4/0 |
| 5 | action-only | 5,357.0 | 0.0 | 0 | 0 | 0/0 |

### Chrome

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 7,013.7 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 4,883.7 | 21,884.2 | 163,624 | 50,756 | 4/0 |
| 1 | auto | 5,351.8 | 9,121.8 | 63,582 | 19,906 | 3/1 |
| 2 | full | 5,796.6 | 8,220.6 | 66,281 | 20,682 | 4/0 |
| 2 | auto | 5,944.0 | 9,541.4 | 63,564 | 19,898 | 3/1 |
| 2 | action-only | 6,074.5 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 5,532.9 | 9,119.7 | 63,665 | 19,932 | 3/1 |
| 3 | action-only | 5,920.2 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 5,969.0 | 9,417.4 | 65,839 | 20,544 | 4/0 |
| 4 | action-only | 5,935.4 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 5,751.5 | 8,242.2 | 66,281 | 20,682 | 4/0 |
| 4 | auto | 5,750.0 | 21,451.4 | 164,748 | 51,987 | 4/0 |
| 5 | full | 5,687.3 | 8,211.3 | 66,281 | 20,682 | 4/0 |
| 5 | auto | 5,365.4 | 21,630.2 | 164,782 | 52,005 | 4/0 |
| 5 | action-only | 5,609.3 | 0.0 | 0 | 0 | 0/0 |

### Word

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 38.4 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 8.6 | 1,797.3 | 9,128 | 2,856 | 4/0 |
| 1 | auto | 6.9 | 1,719.0 | 1,440 | 396 | 0/4 |
| 2 | full | 6.9 | 1,785.5 | 9,128 | 2,848 | 4/0 |
| 2 | auto | 7.3 | 1,667.0 | 1,436 | 404 | 0/4 |
| 2 | action-only | 11.4 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 7.0 | 1,682.3 | 1,436 | 392 | 0/4 |
| 3 | action-only | 17.4 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 7.9 | 1,734.7 | 9,200 | 2,856 | 4/0 |
| 4 | action-only | 16.9 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 8.4 | 1,886.9 | 9,200 | 2,800 | 4/0 |
| 4 | auto | 8.0 | 1,692.8 | 1,440 | 404 | 0/4 |
| 5 | full | 7.7 | 1,808.2 | 9,204 | 2,836 | 4/0 |
| 5 | auto | 8.2 | 1,800.6 | 1,440 | 380 | 0/4 |
| 5 | action-only | 11.3 | 0.0 | 0 | 0 | 0/0 |

### Excel

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 29.9 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 6.1 | 3,235.9 | 13,088 | 4,036 | 4/0 |
| 1 | auto | 4.7 | 2,440.3 | 1,352 | 376 | 0/4 |
| 2 | full | 4.9 | 2,495.5 | 13,124 | 4,048 | 4/0 |
| 2 | auto | 4.2 | 2,479.2 | 1,356 | 376 | 0/4 |
| 2 | action-only | 7.7 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 3.8 | 2,338.6 | 1,356 | 376 | 0/4 |
| 3 | action-only | 4.5 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 5.5 | 2,649.8 | 13,248 | 4,048 | 4/0 |
| 4 | action-only | 4.6 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 4.6 | 2,530.2 | 13,252 | 4,028 | 4/0 |
| 4 | auto | 3.8 | 2,494.3 | 1,352 | 384 | 0/4 |
| 5 | full | 4.9 | 2,516.2 | 13,248 | 4,084 | 4/0 |
| 5 | auto | 3.4 | 2,403.5 | 1,352 | 380 | 0/4 |
| 5 | action-only | 6.8 | 0.0 | 0 | 0 | 0/0 |

## Reproduce

The benchmarks require an interactive Windows desktop. Chrome is opt-in, and Word or Excel tests
skip explicitly when the corresponding desktop application is unavailable. Run the workloads
sequentially because they share the foreground desktop:

```powershell
$project = '.\tests\Sbroenne.WindowsMcp.Tests\Sbroenne.WindowsMcp.Tests.csproj'
$env:MCP_TEST_CHROME = '1'
$env:MCP_SNAPSHOT_BENCHMARK_OUTPUT = "$env:TEMP\mcp-windows-snapshot-benchmark"

dotnet build $project
dotnet test $project --no-build --filter 'FullyQualifiedName~ElectronSnapshotBenchmarkTests'
dotnet test $project --no-build --filter 'FullyQualifiedName~ChromiumSnapshotBenchmarkTests&DisplayName~Edge'
dotnet test $project --no-build --filter 'FullyQualifiedName~ChromiumSnapshotBenchmarkTests&DisplayName~Chrome'
dotnet test $project --no-build --filter 'FullyQualifiedName~OfficeSnapshotBenchmarkTests&DisplayName~Word'
dotnet test $project --no-build --filter 'FullyQualifiedName~OfficeSnapshotBenchmarkTests&DisplayName~Excel'
```

Each test writes a Markdown report containing medians and raw samples to
`MCP_SNAPSHOT_BENCHMARK_OUTPUT`.

## Limitations

- Live GitHub content, network conditions, application builds, accessibility trees, and machine
  load vary. This benchmark characterizes these runs; it is not a fixed performance promise.
- The browser accessibility trees varied substantially between runs, especially in Chrome. The
  safe fallback prevented an oversized or structurally unsafe diff from being returned. Browser
  savings therefore require a stable same-page or scoped observation, not full-page navigation.
- Office actions edit temporary local files through keyboard input. They do not exercise every
  ribbon, dialog, formula, or workbook feature.
- Electron uses the repository's deterministic harness rather than a large third-party Electron
  application, but its navigation changes real renderer accessibility state.
- Payload savings do not imply snapshot-latency savings because automatic mode must still capture
  the current full tree before computing the response.

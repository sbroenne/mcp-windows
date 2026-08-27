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
| GitHub repository navigation | Edge 152.0.4191.41 | 113,440 | 78,146 | n/a | 35,489 | 24,315 | n/a | 20/0 |
| GitHub repository navigation | Chrome 151.0.7922.174 | 172,795 | 172,838 | n/a | 53,950 | 54,417 | n/a | 20/0 |
| Word document editing | Word 16.0.20326.20100 | 9,200 | 1,440 | 84.3% | 2,848 | 396 | 86.1% | 0/20 |
| Excel worksheet editing | Excel 16.0.20326.20100 | 13,248 | 1,352 | 89.8% | 4,048 | 376 | 90.7% | 0/20 |
| **Mode-effect aggregate** | | **372,944** | **292,758** | **21.5%** | **115,439** | **91,306** | **20.9%** | **40/60** |

Bytes and tokens are medians of the total payload for four post-action snapshots in one run. The
mode-effect aggregate sums the five full medians and substitutes the corresponding full median for
an automatic arm that returned no diffs. This assigns zero incremental savings to full-only
workloads instead of misclassifying live-site variance as a mode effect. Because every workload has
four observations, each workflow has equal weight. Tokens are a SharpToken `cl100k_base`
approximation, not universal model billing tokens.

The measured benefit is strong but not universal. Electron, Word, and Excel produced a diff after
every action, reducing median payloads by 84-94%. Both live GitHub navigation runs selected the safe
full-response fallback every time. Their observed full and automatic payloads differ because GitHub's
live accessibility tree varied between arms, so no browser-navigation savings percentage is reported.
Across this deliberately mixed workload set, the normalized mode effect was 21.5% by bytes and 20.9%
by approximate tokens. A separate regression confirms that a same-page GitHub search-field edit
returns a scoped diff in both Edge and Chrome; it is not included as another benchmark workload.

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

The next safe experiment is therefore post-capture cleanup, not Windows content-view filtering:
remove only unnamed layout containers that are proven to have no action of their own, keep all their
children, and continue returning a full snapshot whenever duplicate controls cannot be matched
one-to-one. This requires retaining cached action-capability information during tree construction;
click coordinates alone are not proof that a UI Automation element is actionable.

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
  Pull requests, Actions, and Code in isolated browser profiles.
- **Word:** edit, append to, undo in, and edit a dedicated temporary RTF document.
- **Excel:** enter four values into a dedicated temporary CSV workbook.

| Workload | Action-only ms | Full snapshot ms | Auto snapshot ms |
|---|---:|---:|---:|
| Electron | 928.1 | 5,037.4 | 5,023.0 |
| Edge | 6,250.1 | 10,432.3 | 7,483.8 |
| Chrome | 7,102.8 | 16,403.1 | 16,518.8 |
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
| 1 | action-only | 8,773.0 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 6,183.0 | 7,793.6 | 77,133 | 23,507 | 4/0 |
| 1 | auto | 5,783.6 | 7,517.1 | 77,767 | 23,867 | 4/0 |
| 2 | full | 7,572.8 | 10,432.3 | 113,440 | 35,489 | 4/0 |
| 2 | auto | 5,899.8 | 7,502.0 | 78,146 | 24,315 | 4/0 |
| 2 | action-only | 6,250.1 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 5,949.3 | 7,003.8 | 78,002 | 24,261 | 4/0 |
| 3 | action-only | 5,944.9 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 6,960.0 | 10,616.8 | 113,631 | 35,552 | 4/0 |
| 4 | action-only | 6,163.8 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 6,919.5 | 10,774.4 | 113,636 | 35,553 | 4/0 |
| 4 | auto | 6,169.0 | 7,334.9 | 78,247 | 24,350 | 4/0 |
| 5 | full | 5,932.8 | 7,426.0 | 78,357 | 24,375 | 4/0 |
| 5 | auto | 5,784.2 | 7,483.8 | 78,419 | 24,398 | 4/0 |
| 5 | action-only | 6,359.0 | 0.0 | 0 | 0 | 0/0 |

### Chrome

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 7,304.0 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 6,795.9 | 15,661.7 | 173,877 | 53,950 | 4/0 |
| 1 | auto | 7,028.7 | 15,330.7 | 180,674 | 56,868 | 4/0 |
| 2 | full | 6,831.1 | 16,400.5 | 172,795 | 54,400 | 4/0 |
| 2 | auto | 7,083.1 | 16,518.8 | 172,838 | 54,417 | 4/0 |
| 2 | action-only | 7,382.6 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 6,953.6 | 16,931.8 | 171,350 | 54,029 | 4/0 |
| 3 | action-only | 6,991.2 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 6,630.2 | 16,745.5 | 170,335 | 53,694 | 4/0 |
| 4 | action-only | 7,027.2 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 7,275.9 | 17,990.8 | 184,807 | 57,722 | 4/0 |
| 4 | auto | 7,618.8 | 16,310.5 | 159,729 | 49,996 | 4/0 |
| 5 | full | 7,517.2 | 16,403.1 | 159,594 | 49,950 | 4/0 |
| 5 | auto | 6,667.5 | 18,039.1 | 184,929 | 57,751 | 4/0 |
| 5 | action-only | 7,102.8 | 0.0 | 0 | 0 | 0/0 |

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

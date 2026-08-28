# Incremental UI snapshot benchmark

Incremental snapshots exist to reduce the repeated UI context sent to an agent. A complete
accessibility tree is still captured on every request, but `mode=auto` compares it with the
remembered semantic tree and returns `kind=diff` only when the change list is safe and less than 80%
of the complete semantic response. Otherwise it returns `kind=full` with a complete simplified view.
Explicit `mode=full` calls remain unchanged. This preserves correctness while making payload savings
workload-dependent rather than guaranteed.

The full response contains the documented compact `tree` and no longer serializes the former
redundant full-detail `elements` copy. Consumers that relied on that duplicate should migrate to
`tree`, or call `ui_find` for a flat result.

## Result

Four representative workflows were run five times per comparison arm on Windows
`10.0.26220.0`. The browser pages were the public `microsoft/vscode` GitHub repository, not a
synthetic TodoMVC page.

| Workload | Environment | Byte savings | Token savings | Auto full/diff |
|---|---|---:|---:|---:|
| Electron form navigation | Electron 44.0.0 | 95.2% | 95.7% | 0/20 |
| GitHub repository navigation | Chrome 151.0.7922.174 | 13.1% | 13.4% | 18/2 |
| Word document editing | Word 16.0.20326.20100 | 84.4% | 85.7% | 0/20 |
| Excel worksheet editing | Excel 16.0.20326.20100 | 89.8% | 90.8% | 0/20 |
| **Equal-workload average** | | **70.6%** | **71.4%** | **18/62** |

Chrome uses the median of five paired run-level reductions: every automatic run is compared with the
complete trees captured by those same requests. This prevents live GitHub variation between
separately launched arms from becoming fake savings. The deterministic Electron and Office rows use
their full-arm medians, whose payloads vary negligibly. The final row averages the four workload
percentages so each workflow has equal weight. Tokens are a SharpToken `cl100k_base` approximation,
not universal model billing tokens.

The measured benefit is strong but not universal. Electron, Word, and Excel produced a diff after
every action, reducing median payloads by 84-96%. Chrome returned two diffs and 18 complete simplified
responses. Removing layout-only containers from those complete automatic responses improved Chrome
from the previous 3.9% byte reduction to 13.1%, and from 3.6% to 13.4% for approximate tokens. Across
the four equally weighted workloads, the average reductions were 70.6% and 71.4%. A separate
regression confirms that a same-page GitHub search-field edit returns a scoped diff in both Edge and
Chrome; it is not included as another benchmark workload.

## Further Chromium cleanup

A second five-run Chrome experiment measured conservative display cleanup separately from the
semantic wrapper removal above. Readiness now requires an exact page control returned by Windows UI
Automation; a partial match against the browser tab title no longer counts as a ready webpage. The
run used Chrome for Testing `151.0.7922.174` and the same four GitHub destinations.

| Comparison | Byte savings | Token savings | Auto full/diff |
|---|---:|---:|---:|
| Display cleanup versus the same automatic semantic responses before cleanup | 10.6% | 13.6% | 20/0 |
| Cleaned automatic responses versus the same raw full captures | 16.5% | 19.3% | 20/0 |

The first row is the decision metric: cleanup alone exceeded the 5% keep threshold. It removes click
coordinates from leaf items that Windows says cannot be acted on. It also removes a child label that
exactly repeats its parent's label, and blank leaf images, but only when they have no action, state,
developer identifier, or children. Readable text, values, toggle state, controls, and element IDs
remain. Explicit `mode=full` output is unchanged.

All 20 responses were complete automatic views because each action navigated to a different page.
That makes this a useful worst case: the savings do not depend on a navigation being mistaken for a
small update. Each automatic capture was also serialized before display cleanup, so live page
variation cannot become fake savings.

| Sample | Before-cleanup bytes | Cleaned bytes | Before-cleanup tokens | Cleaned tokens |
|---:|---:|---:|---:|---:|
| 1 | 203,733 | 181,985 | 64,092 | 55,398 |
| 2 | 205,271 | 183,416 | 64,547 | 55,806 |
| 3 | 207,081 | 185,179 | 64,504 | 55,764 |
| 4 | 204,782 | 183,025 | 63,892 | 55,191 |
| 5 | 187,203 | 167,313 | 58,206 | 50,304 |

Two other Chromium experiments were rejected:

- **Page-only snapshots:** Chrome 152 exposed the address-bar popup as a webpage root in one run,
  while Edge 152 exposed no dependable webpage root. A screen-position fallback could select another
  application covering the page. Because this could return the wrong content, page-only scope is not
  shipped and whole-window snapshots remain the default.
- **Cache-only Windows elements:** asking Windows to return cached properties without live automation
  objects made the Electron safety test fail with `0x80004005`. The speed comparison was stopped
  because correctness had already failed; normal cached tree capture remains in use.

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

Playwright's [ARIA snapshot implementation][playwright-aria] suggested the safer direction now used.
It creates
a small role, name, text, and state tree rather than comparing raw browser nodes. Its
[distiller][playwright-distiller] joins adjacent text, normalizes whitespace, removes empty text, and
unwraps low-information layout containers with one child. Action references are handled separately
and are renewed after navigation. Playwright's loose role-and-name matching is suitable for test
assertions, but not for carrying an action ID across duplicate controls.

We first tested post-capture cleanup rather than Windows content-view filtering. That experiment
unwrapped only unnamed one-child `Pane` and `Group` containers when the
wrapper and child had the same bounds, visibility, and enabled state, the wrapper had no developer
ID, and Windows reported no supported action pattern. It preserved GitHub's Code and Issues controls
in both browsers, but still produced 40 complete responses and no diffs. Checking action patterns
also added provider calls to each candidate wrapper. The cleanup was rejected because it added work
without improving incremental responses.

The production approach instead keeps an internal comparison view and a smaller response view from
the same capture. Explicit full mode retains the complete Windows accessibility tree and all IDs.
Automatic mode removes unnamed `Pane` and `Group` containers only when they have no developer ID and
no direct Invoke, Expand/Collapse, Selection, Toggle, or Value action. It then applies the conservative
display cleanup measured above. Chromium's widely reported `ScrollItem` capability merely brings a
node into view, so it does not make an otherwise anonymous wrapper a user-facing action. Named
controls and direct action references remain in the tree, and uncertain duplicate controls still
force a complete response. A live Chrome test checks that GitHub page controls survive this
projection.

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
- **Chrome:** navigate the public `microsoft/vscode` GitHub repository through Issues, Pull requests,
  Actions, and Code in an isolated browser profile. The scenario uses depth 20 so the snapshot
  includes the webpage, not only the browser frame. Edge and Chrome share Chromium, so the expensive
  benchmark uses Chrome only; short live regressions still cover both because their Windows
  accessibility output is not identical.
- **Word:** edit, append to, undo in, and edit a dedicated temporary RTF document.
- **Excel:** enter four values into a dedicated temporary CSV workbook.

| Workload | Action-only ms | Full snapshot ms | Auto snapshot ms |
|---|---:|---:|---:|
| Electron | 956.3 | 5,045.4 | 4,932.6 |
| Chrome | 6,786.2 | 3,206.9 | 15,104.0 |
| Word | 10.4 | 1,771.0 | 1,730.1 |
| Excel | 7.8 | 2,630.4 | 2,503.2 |

These are median totals for four actions or snapshots, not per-call values. Automatic mode still
captures a complete accessibility tree before comparing it, so it is designed to reduce response
payload and agent context, not capture time.

## Raw samples

Each row is one complete four-action run. `Full/diff` counts the response kinds returned during
that run.

### Electron

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 1,237.6 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 972.5 | 4,983.1 | 64,441 | 19,152 | 4/0 |
| 1 | auto | 928.2 | 5,118.5 | 3,087 | 831 | 0/4 |
| 2 | full | 984.3 | 4,973.7 | 64,441 | 19,152 | 4/0 |
| 2 | auto | 1,003.7 | 4,919.9 | 3,087 | 831 | 0/4 |
| 2 | action-only | 935.7 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 890.6 | 4,838.0 | 3,087 | 831 | 0/4 |
| 3 | action-only | 896.9 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 909.2 | 5,045.4 | 64,441 | 19,152 | 4/0 |
| 4 | action-only | 956.3 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 961.3 | 5,326.6 | 64,441 | 19,152 | 4/0 |
| 4 | auto | 967.3 | 4,942.1 | 2,627 | 696 | 0/4 |
| 5 | full | 939.2 | 5,163.6 | 64,441 | 19,152 | 4/0 |
| 5 | auto | 1,009.3 | 4,932.6 | 3,087 | 831 | 0/4 |
| 5 | action-only | 1,058.3 | 0.0 | 0 | 0 | 0/0 |

### Chrome

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Same-capture full bytes | Same-capture full tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | action-only | 9,632.7 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 1 | full | 5,785.1 | 2,944.6 | 15,660 | 4,720 | 15,660 | 4,720 | 4/0 |
| 1 | auto | 6,563.6 | 15,104.0 | 54,696 | 16,717 | 62,954 | 19,294 | 4/0 |
| 2 | full | 5,772.3 | 3,206.9 | 15,761 | 4,690 | 15,761 | 4,690 | 4/0 |
| 2 | auto | 7,455.2 | 6,597.3 | 59,055 | 17,869 | 65,880 | 19,968 | 4/0 |
| 2 | action-only | 10,031.3 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | auto | 6,322.9 | 3,256.8 | 8,340 | 2,462 | 16,900 | 5,143 | 3/1 |
| 3 | action-only | 6,447.9 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | full | 5,852.7 | 15,541.8 | 64,888 | 20,339 | 64,888 | 20,339 | 4/0 |
| 4 | action-only | 5,565.2 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 4 | full | 6,328.0 | 2,868.6 | 15,865 | 4,829 | 15,865 | 4,829 | 4/0 |
| 4 | auto | 6,384.0 | 24,418.9 | 103,799 | 32,665 | 114,207 | 35,924 | 3/1 |
| 5 | full | 6,316.8 | 23,509.7 | 112,702 | 35,470 | 112,702 | 35,470 | 4/0 |
| 5 | auto | 6,486.5 | 15,353.4 | 55,431 | 17,313 | 63,864 | 19,989 | 4/0 |
| 5 | action-only | 6,786.2 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |

### Word

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 22.4 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 8.0 | 1,638.6 | 9,236 | 2,856 | 4/0 |
| 1 | auto | 7.5 | 1,787.7 | 1,440 | 392 | 0/4 |
| 2 | full | 8.5 | 1,877.0 | 9,236 | 2,792 | 4/0 |
| 2 | auto | 7.8 | 1,691.8 | 1,440 | 400 | 0/4 |
| 2 | action-only | 9.9 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 7.0 | 1,730.1 | 1,440 | 404 | 0/4 |
| 3 | action-only | 10.4 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 7.8 | 1,802.8 | 9,236 | 2,792 | 4/0 |
| 4 | action-only | 8.0 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 10.3 | 1,719.2 | 9,236 | 2,792 | 4/0 |
| 4 | auto | 7.0 | 1,671.0 | 1,440 | 400 | 0/4 |
| 5 | full | 6.7 | 1,771.0 | 9,196 | 2,784 | 4/0 |
| 5 | auto | 8.9 | 1,742.1 | 1,440 | 392 | 0/4 |
| 5 | action-only | 11.2 | 0.0 | 0 | 0 | 0/0 |

### Excel

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 29.7 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 6.0 | 3,290.4 | 13,088 | 4,100 | 4/0 |
| 1 | auto | 4.8 | 2,480.4 | 1,356 | 368 | 0/4 |
| 2 | full | 5.5 | 2,487.3 | 13,128 | 4,080 | 4/0 |
| 2 | auto | 4.1 | 2,485.7 | 1,356 | 376 | 0/4 |
| 2 | action-only | 7.3 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 4.1 | 2,522.5 | 1,352 | 380 | 0/4 |
| 3 | action-only | 8.5 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 5.0 | 2,687.1 | 13,252 | 4,080 | 4/0 |
| 4 | action-only | 7.8 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 4.0 | 2,630.4 | 13,252 | 4,036 | 4/0 |
| 4 | auto | 4.4 | 2,525.6 | 1,356 | 392 | 0/4 |
| 5 | full | 5.2 | 2,501.1 | 13,252 | 4,060 | 4/0 |
| 5 | auto | 3.8 | 2,503.2 | 1,356 | 372 | 0/4 |
| 5 | action-only | 3.9 | 0.0 | 0 | 0 | 0/0 |

## Reproduce

The benchmarks require an interactive Windows desktop. Chrome is opt-in, and Word or Excel tests
skip explicitly when the corresponding desktop application is unavailable. Run the workloads
sequentially because they share the foreground desktop:

```powershell
$project = '.\tests\Sbroenne.WindowsMcp.Tests\Sbroenne.WindowsMcp.Tests.csproj'
$env:MCP_TEST_CHROME = '1'
# Optional: pin an official build when the installed Chromium version does not expose page controls.
# $env:MCP_TEST_CHROME_PATH = 'C:\path\to\chrome.exe'
$env:MCP_SNAPSHOT_BENCHMARK_OUTPUT = "$env:TEMP\mcp-windows-snapshot-benchmark"

dotnet build $project
dotnet test $project --no-build --filter 'FullyQualifiedName~ElectronSnapshotBenchmarkTests'
dotnet test $project --no-build --filter 'FullyQualifiedName~Benchmark_PublicGitHubRepositoryWorkflow_Chrome'
dotnet test $project --no-build --filter 'FullyQualifiedName~OfficeSnapshotBenchmarkTests&DisplayName~Word'
dotnet test $project --no-build --filter 'FullyQualifiedName~OfficeSnapshotBenchmarkTests&DisplayName~Excel'
```

Each test writes a Markdown report containing medians and raw samples to
`MCP_SNAPSHOT_BENCHMARK_OUTPUT`.

## Limitations

- Live GitHub content, network conditions, application builds, accessibility trees, and machine
  load vary. This benchmark characterizes these runs; it is not a fixed performance promise.
- Chrome's live accessibility tree varied substantially between runs. The safe fallback prevented an
  oversized or structurally unsafe diff from being returned; the semantic projection reduces these
  complete automatic responses without pretending that a full-page navigation was a small change.
- Office actions edit temporary local files through keyboard input. They do not exercise every
  ribbon, dialog, formula, or workbook feature.
- Electron uses the repository's deterministic harness rather than a large third-party Electron
  application, but its navigation changes real renderer accessibility state.
- Payload savings do not imply snapshot-latency savings because automatic mode must still capture
  the current full tree before computing the response.

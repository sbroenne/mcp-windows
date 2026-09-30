# Incremental UI snapshot benchmark

A snapshot describes the controls in a window. Automatic snapshots can send just
the changes since the last view, rather than repeating the whole window.

The server still reads the complete window before comparing it. It sends changes
only when they are safe to apply and take less than 80% of a complete automatic
response. Otherwise it sends a complete view. Explicit `mode=full` requests
always return the full tree of controls.

## Recorded measurements: 27 September 2026

These runs used source revision
[`67ba78d6b3a99bdabee27898dab64b38aafb55ed`](https://github.com/sbroenne/mcp-windows/commit/67ba78d6b3a99bdabee27898dab64b38aafb55ed),
Release mode, .NET SDK 10.0.401, and Windows `10.0.26220.0`.
The Word run includes the document-startup fix in this change: wait for the
document window, not the temporary "Opening" window.
The Chrome run used an earlier implementation that removed a selected
address-completion suggestion inside the typing tool. Current typing reports
that mismatch without changing it. The benchmark caller now explicitly checks
the selected suggestion before rejecting it and verifies the final URL.
The Chrome figures below are retained as earlier snapshot measurements, not
validation or timing measurements of the current typing workflow.
The tests ran one at a time on a Windows desktop, using temporary test content.

| Workload | Environment | Fewer bytes | Fewer approximate tokens | Full views / changes-only views |
|----------|-------------|------------:|-------------------------:|-------------------------------:|
| Electron test app navigation | Electron 44.2.0 | 95.6% | 96.6% | 0/20 |
| Excel worksheet editing | Excel 16.0.20326.20158 | 81.0% | 84.0% | 0/20 |
| GitHub repository navigation | Chrome 154.0.8037.57 | 10.6% | 11.3% | 20/0 |
| Word document editing | Word 16.0.20326.20158 | 82.7% | 86.1% | 0/20 |

Chrome completed all 15 runs: five samples in each of the three modes, covering
60 page changes. All 20 measured automatic replies were complete views, not
changes-only replies. Their 11.3% token saving came from the more compact
automatic output. This page-to-page workload behaves differently from small
edits within the same window.

### What the percentages mean

Each workload runs five times in each of three modes:

1. **Action-only:** perform four actions without taking snapshots. This measures
   action time, not information savings.
2. **Full:** request a complete snapshot after each action.
3. **Auto:** take one initial snapshot, then request changes after each action.
   The initial snapshot is not included in the measured totals.

For each auto run, the test also saves the same captured controls as full
responses. The savings compare those two versions of the same captures.
The reported percentage is the median of the five run-level percentages.
This avoids treating differences between separate live webpages as savings.

**Tokens** are the small pieces of text an AI model processes. Counts here use
SharpToken's `cl100k_base` tokenizer as an approximation. They do not measure
the cost of a whole agent conversation, and other models may count differently.

### Timing and response sizes

These are median totals for four actions or snapshots, not per-call times.

| Workload | Mode | Action ms | Snapshot ms | Bytes | Approximate tokens |
|----------|------|----------:|------------:|------:|-------------------:|
| Electron | action-only | 2663.7 | 0.0 | 0 | 0 |
| Electron | full | 3341.1 | 7909.3 | 80385 | 30928 |
| Electron | auto | 2912.8 | 6879.2 | 3543 | 1054 |
| Excel | action-only | 8.8 | 0.0 | 0 | 0 |
| Excel | full | 23.0 | 6465.8 | 15996 | 6261 |
| Excel | auto | 7.9 | 4296.9 | 3032 | 1005 |
| Chrome | action-only | 25165.4 | 0.0 | 0 | 0 |
| Chrome | full | 20494.1 | 34256.9 | 312581 | 124179 |
| Chrome | auto | 22791.2 | 37659.0 | 289265 | 113903 |
| Word | action-only | 21.0 | 0.0 | 0 | 0 |
| Word | full | 15.3 | 3715.7 | 10765 | 4198 |
| Word | auto | 15.6 | 3101.3 | 1864 | 586 |

Smaller responses do not necessarily mean faster capture. Timing depends on the
application, its startup state, and the machine's other work.

## Workloads

- **Electron:** move through Forms, Data, Settings, and Home in the project's
  test application. This does not measure all Electron applications.
- **Chrome:** visit Issues, Pull requests, Actions, and Code in the public
  `microsoft/vscode` GitHub repository, using a separate browser profile.
  The address bar is discovered during setup and its exact ID is reused.
  The current benchmark caller handles a verified selected URL suggestion
  explicitly; it does not repeat a failed navigation.
  Setup observations are outside the measured actions. Measured snapshots
  include the webpage, not just the browser's toolbar.
- **Word:** edit, append text, undo, and edit a temporary RTF document.
- **Excel:** enter four values in a temporary CSV file. This does not test
  formulas, ribbon commands, or every Excel feature.

The order of the three modes rotates between samples. Each starts with equivalent
test content. Response sizes use the server's normal JSON output.

Chrome startup checks read the window once per attempt and require the webpage
controls to appear. Popup handling uses buttons already found in that view,
rather than searching repeatedly for popups that may not exist. Those searches
could previously consume the entire startup waiting time. Startup failures now
record the timing of each attempt and the last window view.

## Raw samples

Each row covers four actions. The "Same-capture full" columns are the complete
versions used to calculate auto savings.

### Electron 44.2.0

| Sample | Mode | Action ms | Snapshot ms | Bytes | Tokens | Same-capture full bytes | Same-capture full tokens | Full/diff |
|-------:|------|----------:|------------:|------:|-------:|-----------------------:|------------------------:|----------:|
| 1 | action-only | 7017.9 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 1 | full | 3015.7 | 7909.3 | 80385 | 30927 | 80385 | 30927 | 4/0 |
| 1 | auto | 2912.8 | 6879.2 | 3543 | 1054 | 80385 | 30927 | 0/4 |
| 2 | full | 2726.3 | 6724.5 | 80385 | 30928 | 80385 | 30928 | 4/0 |
| 2 | auto | 2669.9 | 6786.5 | 3543 | 1055 | 80385 | 30927 | 0/4 |
| 2 | action-only | 2663.7 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | auto | 4099.0 | 8825.2 | 3543 | 1043 | 80385 | 30925 | 0/4 |
| 3 | action-only | 2369.2 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | full | 3763.8 | 10510.2 | 80385 | 30928 | 80385 | 30928 | 4/0 |
| 4 | action-only | 4146.8 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 4 | full | 4791.7 | 10409.4 | 80385 | 30928 | 80385 | 30928 | 4/0 |
| 4 | auto | 5419.0 | 12897.9 | 3543 | 1050 | 80385 | 30926 | 0/4 |
| 5 | full | 3341.1 | 7725.3 | 80385 | 30924 | 80385 | 30924 | 4/0 |
| 5 | auto | 2455.7 | 5933.1 | 3543 | 1054 | 80385 | 30928 | 0/4 |
| 5 | action-only | 2299.8 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |

### Excel 16.0.20326.20158

| Sample | Mode | Action ms | Snapshot ms | Bytes | Tokens | Same-capture full bytes | Same-capture full tokens | Full/diff |
|-------:|------|----------:|------------:|------:|-------:|-----------------------:|------------------------:|----------:|
| 1 | action-only | 110.6 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 1 | full | 70.9 | 7289.2 | 16015 | 6286 | 16015 | 6286 | 4/0 |
| 1 | auto | 19.7 | 6466.3 | 3032 | 1005 | 15980 | 6300 | 0/4 |
| 2 | full | 8.9 | 6465.8 | 15980 | 6261 | 15980 | 6261 | 4/0 |
| 2 | auto | 8.1 | 4832.0 | 3030 | 1002 | 15978 | 6274 | 0/4 |
| 2 | action-only | 29.5 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | auto | 5.6 | 3572.8 | 3029 | 988 | 15977 | 6252 | 0/4 |
| 3 | action-only | 4.2 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | full | 23.0 | 6271.8 | 15980 | 6226 | 15980 | 6226 | 4/0 |
| 4 | action-only | 8.8 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 4 | full | 41.3 | 6526.9 | 15996 | 6213 | 15996 | 6213 | 4/0 |
| 4 | auto | 5.6 | 3673.7 | 3037 | 1005 | 16101 | 6299 | 0/4 |
| 5 | full | 5.3 | 3330.6 | 16101 | 6287 | 16101 | 6287 | 4/0 |
| 5 | auto | 7.9 | 4296.9 | 3038 | 1010 | 16102 | 6314 | 0/4 |
| 5 | action-only | 3.6 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |

### Chrome 154.0.8037.57

| Sample | Mode | Action ms | Snapshot ms | Bytes | Tokens | Same-capture full bytes | Same-capture full tokens | Full/diff |
|-------:|------|----------:|------------:|------:|-------:|-----------------------:|------------------------:|----------:|
| 1 | action-only | 28199.4 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 1 | full | 19267.0 | 28588.1 | 303946 | 119809 | 303946 | 119809 | 4/0 |
| 1 | auto | 16727.7 | 24395.0 | 289519 | 113903 | 323753 | 128347 | 4/0 |
| 2 | full | 16909.0 | 31351.3 | 309501 | 123353 | 309501 | 123353 | 4/0 |
| 2 | auto | 22791.2 | 37659.0 | 289544 | 114786 | 323778 | 129252 | 4/0 |
| 2 | action-only | 26388.5 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | auto | 19097.5 | 23316.1 | 283677 | 112239 | 317911 | 126654 | 4/0 |
| 3 | action-only | 16792.4 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | full | 20494.1 | 34571.7 | 312581 | 124179 | 312581 | 124179 | 4/0 |
| 4 | action-only | 25165.4 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 4 | full | 23491.2 | 34256.9 | 323779 | 127923 | 323779 | 127923 | 4/0 |
| 4 | auto | 24840.9 | 37936.3 | 283702 | 111546 | 317936 | 126034 | 4/0 |
| 5 | full | 25194.7 | 43828.0 | 323782 | 128628 | 323782 | 128628 | 4/0 |
| 5 | auto | 23463.7 | 38803.8 | 289265 | 114123 | 323436 | 128529 | 4/0 |
| 5 | action-only | 22832.2 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |

### Word 16.0.20326.20158

| Sample | Mode | Action ms | Snapshot ms | Bytes | Tokens | Same-capture full bytes | Same-capture full tokens | Full/diff |
|-------:|------|----------:|------------:|------:|-------:|-----------------------:|------------------------:|----------:|
| 1 | action-only | 41.9 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 1 | full | 12.6 | 3471.2 | 10746 | 4198 | 10746 | 4198 | 4/0 |
| 1 | auto | 31.3 | 4110.3 | 1866 | 589 | 10766 | 4238 | 0/4 |
| 2 | full | 13.5 | 3720.7 | 10766 | 4154 | 10766 | 4154 | 4/0 |
| 2 | auto | 15.6 | 3435.4 | 1861 | 589 | 10761 | 4213 | 0/4 |
| 2 | action-only | 39.5 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | auto | 13.6 | 2560.9 | 1864 | 556 | 10764 | 4132 | 0/4 |
| 3 | action-only | 14.2 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 3 | full | 15.4 | 3611.0 | 10765 | 4213 | 10765 | 4213 | 4/0 |
| 4 | action-only | 21.0 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |
| 4 | full | 24.6 | 3715.7 | 10762 | 4198 | 10762 | 4198 | 4/0 |
| 4 | auto | 20.8 | 3078.7 | 1866 | 586 | 10766 | 4186 | 0/4 |
| 5 | full | 15.3 | 3979.0 | 10766 | 4218 | 10766 | 4218 | 4/0 |
| 5 | auto | 13.3 | 3101.3 | 1864 | 564 | 10764 | 4096 | 0/4 |
| 5 | action-only | 14.5 | 0.0 | 0 | 0 | 0 | 0 | 0/0 |

## CLI continuity check

Separate `wincli` commands can share the same remembered view. Pass the returned
`snapshotToken` as `--since` on the next automatic snapshot. A missing or outdated
token returns a complete view.

On 27 September 2026, a Release-mode check against the project's Windows test app
returned:

| Response | UTF-8 bytes | End-to-end CLI call |
|----------|------------:|-------------------:|
| First automatic snapshot | 5,861 | 5,266.7 ms |
| Next snapshot with matching token | 251 | 460.6 ms |

That unchanged-window response was **95.7% smaller**. Missing and interleaved
tokens returned complete views as required. Both continuity tests passed,
including rejecting an old ID when a control with the same label replaced it.
The test's CLI service was stopped afterward.

These are single-check measurements, not a multi-application benchmark or a
cost estimate. The first call includes startup only if the service was not
already running.

## Reproduce

Run on a Windows desktop that the tests can use without interruption. They take
control of the mouse and keyboard. Chrome, Word, and Excel must be installed for
their workloads. Tests explicitly skip an unavailable application.

Run the workloads one at a time:

```powershell
$project = '.\tests\Sbroenne.WindowsMcp.Tests\Sbroenne.WindowsMcp.Tests.csproj'
$env:MCP_TEST_CHROME = '1'
$env:MCP_SNAPSHOT_BENCHMARK_OUTPUT = "$env:TEMP\mcp-windows-snapshot-benchmark"

dotnet build $project -c Release
dotnet test $project -c Release --no-build --filter 'FullyQualifiedName~SnapshotBenchmarkRunnerTests'
dotnet test $project -c Release --no-build --filter 'FullyQualifiedName~ElectronSnapshotBenchmarkTests'
dotnet test $project -c Release --no-build --filter 'FullyQualifiedName~Benchmark_PublicGitHubRepositoryWorkflow_Chrome'
dotnet test $project -c Release --no-build --filter 'FullyQualifiedName~Benchmark_RealOfficeWorkflow&DisplayName~Word'
dotnet test $project -c Release --no-build --filter 'FullyQualifiedName~Benchmark_RealOfficeWorkflow&DisplayName~Excel'
dotnet test $project -c Release --no-build --filter 'FullyQualifiedName~CliSnapshotContinuityTests'
```

Each benchmark writes a Markdown report with medians and raw samples to
`MCP_SNAPSHOT_BENCHMARK_OUTPUT`.

## Limits

Live webpages, application versions, network conditions, and machine load vary.
These results describe the listed runs, not a guaranteed saving for every task.
Large changes may need a complete view. Smaller responses do not necessarily
mean faster capture or lower total agent costs.

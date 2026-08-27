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
| Electron form navigation | Electron 44.0.0 | 63,777 | 63,777 | 0.0% | 18,976 | 18,976 | 0.0% | 20/0 |
| GitHub repository navigation | Edge 152.0.4191.41 | 78,989 | 78,913 | 0.1% | 24,641 | 24,608 | 0.1% | 20/0 |
| GitHub repository navigation | Chrome 151.0.7922.174 | 172,795 | 172,838 | -0.0% | 53,950 | 54,417 | -0.9% | 20/0 |
| Word document editing | Word 16.0.20326.20100 | 9,184 | 1,436 | 84.4% | 2,808 | 392 | 86.0% | 0/20 |
| Excel worksheet editing | Excel 16.0.20326.20100 | 13,204 | 1,352 | 89.8% | 4,036 | 376 | 90.7% | 0/20 |
| **Equal-workload aggregate** | | **337,949** | **318,316** | **5.8%** | **104,411** | **98,769** | **5.4%** | **60/40** |

Bytes and tokens are medians of the total payload for four post-action snapshots in one run. The
aggregate sums the five workload medians; because every workload has four observations, this gives
each workflow equal weight. Tokens are a SharpToken `cl100k_base` approximation, not universal
model billing tokens.

The measured benefit is strong but not universal. Word and Excel produced a diff after every
action, reducing median payloads by 84-90%. Electron and both live GitHub browser runs selected the
safe full-response fallback every time, so they produced no meaningful payload savings. The small
positive or negative browser differences are normal variation in live GitHub content, not
incremental savings. Across this deliberately mixed workload set, median payload savings were 5.8%
by bytes and 5.4% by approximate tokens. The earlier 82% single-response and 91% short-workflow
figures should therefore not be generalized across applications.

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
| Electron | 955.1 | 5,069.1 | 5,012.8 |
| Edge | 6,465.8 | 8,281.5 | 8,002.0 |
| Chrome | 7,102.8 | 16,403.1 | 16,518.8 |
| Word | 13.6 | 2,321.6 | 2,087.8 |
| Excel | 19.0 | 3,032.6 | 2,980.3 |

These are median totals for four actions or snapshots, not per-call values. Automatic mode still
captures a complete accessibility tree before comparing it, so it is designed to reduce response
payload and agent context, not capture time.

## Raw samples

Each row is one complete four-action run. `Full/diff` counts the response kinds returned during
that run.

### Electron

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 1,145.9 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 964.7 | 5,069.1 | 63,777 | 18,976 | 4/0 |
| 1 | auto | 1,017.8 | 5,012.8 | 63,777 | 18,976 | 4/0 |
| 2 | full | 914.4 | 4,974.5 | 63,777 | 18,976 | 4/0 |
| 2 | auto | 1,025.0 | 5,167.9 | 63,777 | 18,976 | 4/0 |
| 2 | action-only | 966.3 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 1,016.5 | 4,950.3 | 63,777 | 18,976 | 4/0 |
| 3 | action-only | 955.1 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 940.0 | 5,100.0 | 63,777 | 18,976 | 4/0 |
| 4 | action-only | 954.6 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 954.4 | 5,089.8 | 63,777 | 18,976 | 4/0 |
| 4 | auto | 921.6 | 5,014.9 | 63,777 | 18,976 | 4/0 |
| 5 | full | 913.6 | 4,951.7 | 63,777 | 18,976 | 4/0 |
| 5 | auto | 877.9 | 4,856.5 | 63,773 | 18,976 | 4/0 |
| 5 | action-only | 933.4 | 0.0 | 0 | 0 | 0/0 |

### Edge

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 9,263.9 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 5,971.0 | 8,279.9 | 77,621 | 23,700 | 4/0 |
| 1 | auto | 5,991.3 | 8,002.0 | 78,338 | 24,118 | 4/0 |
| 2 | full | 5,785.6 | 8,386.4 | 78,820 | 24,577 | 4/0 |
| 2 | auto | 5,814.5 | 8,107.7 | 78,913 | 24,608 | 4/0 |
| 2 | action-only | 6,307.3 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 6,013.3 | 7,682.5 | 78,881 | 24,597 | 4/0 |
| 3 | action-only | 6,215.9 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 6,122.7 | 8,281.5 | 78,989 | 24,641 | 4/0 |
| 4 | action-only | 6,465.8 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 7,060.2 | 11,127.4 | 115,096 | 36,121 | 4/0 |
| 4 | auto | 7,355.8 | 11,198.1 | 115,103 | 36,124 | 4/0 |
| 5 | full | 6,207.1 | 8,210.4 | 79,123 | 24,678 | 4/0 |
| 5 | auto | 6,066.0 | 7,562.7 | 79,071 | 24,660 | 4/0 |
| 5 | action-only | 6,477.4 | 0.0 | 0 | 0 | 0/0 |

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
| 1 | action-only | 59.0 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 11.5 | 2,296.4 | 9,128 | 2,840 | 4/0 |
| 1 | auto | 9.6 | 2,182.3 | 1,436 | 388 | 0/4 |
| 2 | full | 13.4 | 2,745.9 | 9,160 | 2,808 | 4/0 |
| 2 | auto | 9.4 | 2,084.9 | 1,436 | 400 | 0/4 |
| 2 | action-only | 13.6 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 9.0 | 2,348.9 | 1,440 | 388 | 0/4 |
| 3 | action-only | 8.6 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 25.9 | 2,781.3 | 9,184 | 2,808 | 4/0 |
| 4 | action-only | 19.1 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 13.0 | 2,293.8 | 9,232 | 2,792 | 4/0 |
| 4 | auto | 8.6 | 1,987.3 | 1,436 | 408 | 0/4 |
| 5 | full | 10.7 | 2,321.6 | 9,232 | 2,760 | 4/0 |
| 5 | auto | 9.7 | 2,087.8 | 1,436 | 392 | 0/4 |
| 5 | action-only | 11.0 | 0.0 | 0 | 0 | 0/0 |

### Excel

| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Full/diff |
|---:|---|---:|---:|---:|---:|---:|
| 1 | action-only | 34.0 | 0.0 | 0 | 0 | 0/0 |
| 1 | full | 9.2 | 3,244.6 | 13,045 | 4,005 | 4/0 |
| 1 | auto | 4.1 | 2,888.2 | 1,352 | 376 | 0/4 |
| 2 | full | 5.5 | 3,032.6 | 13,080 | 4,084 | 4/0 |
| 2 | auto | 4.6 | 2,880.2 | 1,352 | 376 | 0/4 |
| 2 | action-only | 19.0 | 0.0 | 0 | 0 | 0/0 |
| 3 | auto | 6.9 | 4,536.9 | 1,354 | 386 | 0/4 |
| 3 | action-only | 21.4 | 0.0 | 0 | 0 | 0/0 |
| 3 | full | 4.5 | 2,893.1 | 13,204 | 4,088 | 4/0 |
| 4 | action-only | 13.6 | 0.0 | 0 | 0 | 0/0 |
| 4 | full | 4.5 | 3,064.3 | 13,204 | 4,036 | 4/0 |
| 4 | auto | 3.7 | 3,141.1 | 1,352 | 376 | 0/4 |
| 5 | full | 4.1 | 2,843.7 | 13,204 | 4,020 | 4/0 |
| 5 | auto | 3.6 | 2,980.3 | 1,352 | 376 | 0/4 |
| 5 | action-only | 18.4 | 0.0 | 0 | 0 | 0/0 |

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
  safe fallback prevented an oversized or structurally unsafe diff from being returned.
- Office actions edit temporary local files through keyboard input. They do not exercise every
  ribbon, dialog, formula, or workbook feature.
- Electron uses the repository's deterministic harness rather than a large third-party Electron
  application, but its navigation changes real renderer accessibility state.
- Payload savings do not imply snapshot-latency savings because automatic mode must still capture
  the current full tree before computing the response.

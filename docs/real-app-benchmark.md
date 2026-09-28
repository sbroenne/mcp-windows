# Real Windows tasks: controls versus screenshots

This benchmark asks an AI to finish work in **Notepad, Word, PowerPoint, and
Chrome**, then checks the saved result independently. It measures the whole
conversation and its Windows actions, not just reading a saved image.

## Results

**Controls first completed 11 of 16 tasks; screenshots completed 7 of 16.**
The fresh 32-trial comparison finished on 28 September 2026. Saved files and
submitted values were checked independently. All 32 original input files
remained unchanged.

| Model | Controls first | Screenshots |
|---|---:|---:|
| GPT-6 Astra | 4/4 | 4/4 |
| GPT-6 Luna | 3/4 | 2/4 |
| GPT-5.6 Sol | 3/4 | 1/4 |
| GPT-5.6 Luna | 1/4 | 0/4 |
| **Total** | **11/16** | **7/16** |

GPT-6 Astra completed every task by both routes. The other models differed
substantially on these tasks, even though all four had read the simpler
single-field images correctly.

| App | Controls first | Screenshots |
|---|---:|---:|
| Notepad | 4/4 | 3/4 |
| Word | 3/4 | 2/4 |
| PowerPoint | 2/4 | 1/4 |
| Chrome | 2/4 | 1/4 |

### Tokens and time when both routes finished

Seven app/model pairs succeeded by both routes. Across those pairs, controls
first used **51.1% fewer input tokens** and took **49.3% less time** at the
middle of each set of savings. These are medians of seven separate comparisons,
not percentages calculated from all attempted tasks.

| App | Model | Controls input tokens | Screenshot input tokens | Controls seconds | Screenshot seconds |
|---|---|---:|---:|---:|---:|
| Notepad | GPT-6 Astra | 62,034 | 136,411 | 40.1 | 79.1 |
| Notepad | GPT-6 Luna | 106,468 | 824,867 | 56.8 | 220.7 |
| Notepad | GPT-5.6 Sol | 73,070 | 130,146 | 33.1 | 82.1 |
| Word | GPT-6 Astra | 238,674 | 348,135 | 131.3 | 142.1 |
| Word | GPT-6 Luna | 524,164 | 1,072,063 | 156.0 | 280.6 |
| PowerPoint | GPT-6 Astra | 447,658 | 367,153 | 146.4 | 148.5 |
| Chrome | GPT-6 Astra | 209,711 | 779,469 | 91.8 | 212.0 |

Controls first saved input tokens in six of the seven pairs. **PowerPoint was
the exception:** GPT-6 Astra used 21.9% more input tokens with controls, with
almost the same elapsed time. Controls first was faster in all seven pairs,
but not always by much.

### What failed

Twelve trials reached the 80-call limit without a verified saved or submitted
result. Two more finished with incorrect output:

- GPT-5.6 Luna's controls-first Word copy had the correct text, but its title
  still used Normal rather than Heading 1.
- GPT-5.6 Sol's controls-first Chrome submission used year **212026**, not
  2026. The server reported that direct date-field entry could not be verified;
  the agent then used the keyboard and submitted the wrong date.

Both agents claimed success, so the independent checks mattered. The Chrome
date control also exposed a useful limitation: a text read returned its label,
not its current date. Screenshots remained available for checking the value.
These outcomes identify work to investigate; they do not by themselves prove
that every failure was caused by the model or by Windows MCP.

### Complete trial counts

Totals below include failed attempts and must not be read as equal-work
efficiency savings.

| Route | Completed | Reported input tokens | Reported output tokens | Total minutes | Tool calls |
|---|---:|---:|---:|---:|---:|
| Controls first | 11/16 | 12,116,508 | 49,407 | 49.0 | 543 |
| Screenshots | 7/16 | 20,401,136 | 81,066 | 77.0 | 983 |

[Download all 32 trials, request checks, settings, and result hashes](../gh-pages/docs/assets/benchmarks/real-apps.json).
This is one trial per app/model/route: useful evidence about these tasks, not
a general model ranking.

## Tasks

| App | Requested work | Independent check |
|-----|----------------|-------------------|
| Notepad | Change a review date and status, add an action, and save a copy | Exact saved text, with the original unchanged |
| Word | Apply Heading 1, change a date, preserve the other paragraphs and styles, and save a copy | Text and paragraph styles in the saved Word file |
| PowerPoint | Change a title and a bullet, reorder two slides, and save a copy | Saved slide order and text on every slide |
| Chrome | Complete and submit a workshop booking with seven fields, including a department, date, and projector checkbox | Values received by the local booking server |

The agent does not get the complete source document text in its task prompt.
It must inspect the open application, make the changes, and save or submit them.
A claim that the task is finished does not count as success.

## Two routes through the same server

**Controls first:** the agent can find, read, click, type into, and select
Windows controls. It can request complete or changes-only views and use the
file-saving helper. Screenshots, mouse, and keyboard input remain available.

**Screenshots:** the agent gets unannotated images and uses mouse and keyboard
input. It cannot read control names, values, element IDs, or text through the
control tools. It must handle save dialogs itself.

Both routes use Windows MCP. This is a comparison of these two tool sets,
not a direct benchmark of the computer-use implementations in the Copilot app
or Claude. The result includes the benefit of higher-level actions such as
file saving, not just the difference between image and text input.

The controls route exposes `ui_snapshot`, `ui_find`, `ui_read`, `ui_click`,
`ui_type`, `ui_select`, `ui_read_table`, `ui_wait`, and `file_save`. Both routes
have `window_management`, `screenshot_control`, `mouse_control`, and
`keyboard_control`. Batch operations, macros, the clipboard, launching other
apps, shell commands, Office APIs, browser page source, and developer tools
are not available to the agent.

The benchmark adapter restricts actions and window listings to the test-owned
application. It restricts screenshots to that window and returns no control
annotations. The agent can resize or maximize its test window. Only permitted
actions are advertised; held-key operations and global window searches are
excluded. Other Windows MCP tool descriptions and results are retained.
Every supplied tool definition counts toward the measured model input.

Both routes use the same text-entry implementation from `main`. It sends
Unicode keystrokes with a short pause between characters. In supported
Notepad text controls, the server also checks that each character appeared
before continuing, stopping if that check fails. These internal checks do
not return document text to the screenshot-only agent. Neither route uses
the clipboard for typing.

## Models and run order

The matrix has **32 trials**: four apps, four models, and two routes.
The requested model IDs are `gpt-6-astra`, `gpt-6-luna`, `gpt-5.6-sol`, and
`gpt-5.6-luna`. Every trial starts a fresh, tool-restricted Copilot SDK session,
with medium reasoning effort and the same task instructions.

Route order alternates across app/model pairs. The apps run one at a time
on the same desktop. Each pair starts with copies of the same input file.
Windows start fully inside the desktop work area; the agent can change their
size afterward. Application versions and initial bounds are recorded.
Notepad's generated tab is moved into its own window if earlier tabs were
restored. Setup verifies that this window has exactly one document, and
restored windows are hidden from the agent's tool access. User settings and
saved tabs are not deleted to achieve isolation.
If moving the tab changes focus before the drag finishes, setup checks for
the new window without sending another drag. This run resumed three times
after setup stopped before a model trial began. No completed trial was
repeated or removed; the measured server binary and task settings stayed the
same, and each setup-script version was recorded.
Other restored Word and PowerPoint document windows are excluded from the
agent's tools, without deleting recovery files or changing application settings.
Chrome uses a fresh private profile and a local booking page, avoiding
changing public websites or real bookings.

The completed run used Notepad 11.2607.14.0, Word and PowerPoint
16.0.20326.20158, and Chrome 154.0.8037.57. Each application's version stayed
the same throughout its trials.

Screenshots use JPEG quality 60 and native **high** image detail in both
routes. The runner checks the actual outgoing model ID and available tool
names, and records image-detail settings and instruction hashes.

Each trial has the same ten-minute limit and an 80-tool-call limit. A task
that does not finish stays in the results. A broken connection or benchmark
adapter error stops the run instead of being counted as a model failure.
Harness pilots are recorded separately and excluded from the published matrix.

## What is counted

**Completion comes first.** The saved files and submitted form values are
checked outside the model session. The original document must remain unchanged.

**Input tokens** are the sum reported for every model request, including tool
definitions, task text, returned observations, and conversation history sent
again on later requests. **Output tokens** include the model's reported output
across the conversation, not just its final answer. Reported cache and reasoning
counts are stored separately. These are token measurements, not a dollar bill.

**Elapsed time** starts when the task is sent to the model and includes its
decisions, screenshots, control reads, and Windows actions. It excludes
creating the fixtures, opening the app, creating the model session, and
checking the saved result afterward.

**Tool calls, failed calls, and screenshot calls** are recorded separately.
Timeout rows show reported usage; an interrupted request may not report all
of its final usage.

Token and time savings are calculated only for matching app/model pairs
where **both routes completed** within their limits. Failed attempts are
still shown, but a cheap failed attempt is not credited as an efficiency win.
One trial per combination is a useful first comparison, not a reliable
ranking of model speed or success rates across arbitrary Windows work.

## Evidence and privacy

The local run preserves the task prompts, model messages, tool arguments and
replies, screenshots, usage events, checks, and hashes. The public export
includes the settings, counts, request audits, and result hashes, but excludes
desktop images, raw window text, model messages, and local file paths.
Office account details or unrelated file names visible in a save dialog
should not become public benchmark data.

## Reproduce the real-app comparison

Use a Windows desktop that the benchmark can control without interruption.
Install Notepad, Word, PowerPoint, and Chrome. Close existing Notepad, Word,
and PowerPoint instances first; the runner refuses to take over those
applications. Existing Chrome sessions are left alone.

Build the server, install the benchmark's declared Python dependencies, and
authenticate the GitHub CLI or provide `GITHUB_TOKEN`. Never put a token in
source files or reports.

```powershell
dotnet build src\Sbroenne.WindowsMcp -c Release
uv venv .venv-benchmark --python 3.14
uv pip install --python .venv-benchmark\Scripts\python.exe -r scripts\requirements-real-app-benchmark.txt

.venv-benchmark\Scripts\python.exe -m unittest discover -s scripts\tests -p 'test_real_app*.py'

$env:MCP_TEST_DESKTOP_INPUT = '1'
$env:MCP_TEST_NOTEPAD_SERVER = (Resolve-Path 'src\Sbroenne.WindowsMcp\bin\Release\net10.0-windows10.0.22621.0\Sbroenne.WindowsMcp.exe').Path
.venv-benchmark\Scripts\python.exe -m unittest discover -s scripts\tests -p 'test_notepad_keyboard.py'

.venv-benchmark\Scripts\python.exe scripts\benchmark-real-apps.py `
  --server src\Sbroenne.WindowsMcp\bin\Release\net10.0-windows10.0.22621.0\Sbroenne.WindowsMcp.exe `
  --output C:\benchmark-results\real-apps

.venv-benchmark\Scripts\python.exe scripts\summarize-real-apps.py `
  --run C:\benchmark-results\real-apps `
  --output C:\benchmark-results\real-apps-public.json
```

Use `--pilot --apps notepad --models gpt-6-astra` for a separately labelled
harness check. Keep pilots separate from the complete result matrix.
Create a file named `STOP` in the run's output directory to interrupt safely.
An interrupted matrix cannot be exported as a completed benchmark.
After an interruption during application setup, use the same command with
`--resume` to continue. Completed trials, including failures, are retained
without rerunning them. Resuming checks the server, input files, dependencies,
tool definitions, model settings, limits, and original trial order. It records
each runner version and preserves the interrupted setup directory. A model
trial that started but did not finish cannot be retried through this option.
Before sending a task, the runner now writes a start marker to disk. A hard
interruption after that marker blocks resuming the trial even if no model
response or tool call was recorded.

The [single-field reading check](screenshot-ui-automation-benchmark.md) remains
useful as a smaller supporting test. The
[automatic snapshot measurement](incremental-snapshot-benchmark.md) separately
measures repeated control replies without an AI carrying out the task.

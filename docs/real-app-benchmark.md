# Real Windows tasks: controls versus screenshots

This benchmark asks an AI to finish work in **Notepad, Word, PowerPoint, and
Chrome**, then checks the saved result independently. It measures the whole
conversation and its Windows actions, not just reading a saved image.

## Results

The 30 September 2026 run completed all 16 trials. The controls-first route
completed **8 of 8** tasks. The screenshot-only route completed **7 of 8**.
GPT-6 Luna reached the 80-call limit without saving the PowerPoint copy in its
screenshot-only trial. That failure remains part of the result.

Across the seven app/model pairs where both routes completed, controls first
used a median **34.9% fewer input tokens** and took a median **34.3% less
framework session time**.

| App | Model | Controls first | Screenshots only | Controls-first input tokens | Controls-first time |
|-----|-------|----------------|------------------|-----------------------------|---------------------|
| Notepad | GPT-6.1 Sol | Passed | Passed | 70.4% fewer | 51.5% less |
| Notepad | GPT-6 Luna | Passed | Passed | 56.8% fewer | 42.3% less |
| Word | GPT-6.1 Sol | Passed | Passed | 30.8% fewer | 9.4% less |
| Word | GPT-6 Luna | Passed | Passed | 21.5% fewer | 5.9% less |
| PowerPoint | GPT-6.1 Sol | Passed | Passed | 58.1% more | 35.0% more |
| PowerPoint | GPT-6 Luna | Passed | Failed at 80 calls | Not compared | Not compared |
| Chrome | GPT-6.1 Sol | Passed | Passed | 34.9% fewer | 34.3% less |
| Chrome | GPT-6 Luna | Passed | Passed | 88.0% fewer | 79.0% less |

This is one run per combination, not a general model ranking. PowerPoint with
GPT-6.1 Sol is an important counterexample: controls completed the task but
used more tokens and time than screenshots. Token counts measure model input,
not price.

[Download the privacy-safe evidence](../gh-pages/docs/assets/benchmarks/real-apps.json).
It contains settings, counts, hashes, request-audit metadata, and stop reasons.
Raw screenshots, messages, desktop text, and local paths remain private.

## Tasks

| App | Requested work | Independent check |
|-----|----------------|-------------------|
| Notepad | Change a review date and status, add an action, and save a copy | Exact saved text, including the final line break, with the original unchanged |
| Word | Apply Heading 1, change a date, preserve the other paragraphs and styles, and save a copy | Text and paragraph styles in the saved Word file |
| PowerPoint | Change a title and a bullet, reorder two slides, and save a copy | Saved slide order and text on every slide |
| Chrome | Complete and submit a workshop booking with seven fields, including a department, date, and projector checkbox | Values received by the local booking server |

The agent does not get the complete source document text in its task prompt.
It must inspect the open application, make the changes, and save or submit
them. A claim that the task is finished does not count as success.

## Two routes through the same server

**Controls first:** the agent can find, read, click, type into, and select
Windows controls. It can request complete or changes-only views and use the
file-saving helper. Screenshots, mouse, and keyboard input remain available.

**Screenshots:** the agent gets unannotated images and uses mouse and keyboard
input. It cannot read control names, values, element IDs, or text through the
control tools. It must handle save dialogs itself.

Both routes use Windows MCP. This compares two tool sets within the same
product, not separate products.

The benchmark adapter restricts actions, window listings, and screenshots to
the test-owned application. Batch operations, macros, the clipboard, launching
other apps, shell commands, Office APIs, browser page source, and developer
tools are unavailable to the agent.

## Models and run order

The matrix has **16 trials**: four apps, two models, and two routes. The model
IDs are `gpt-6.1-sol` and `gpt-6-luna`. Every trial starts a fresh,
tool-restricted model session with medium reasoning, high-detail images, a
ten-minute limit, and an 80-tool-call limit.

Route order alternates across app/model pairs. The apps run one at a time on
the same reserved desktop. Each pair starts with copies of the same input
file. Notepad, Word, and PowerPoint must not already be open. Chrome uses a
separate test profile and local booking page.

## What is counted

**Completion comes first.** Saved files and submitted form values are checked
outside the model session. The original input must remain unchanged.

The framework records the actual model requests, tools, reasoning setting,
image detail, usage, stop reason, and admitted tool calls. Unsupported or
incomplete request formats stop the run rather than producing incomplete
evidence.

Token and time comparisons are calculated only for matching app/model pairs
where both routes complete within their limits. Failed attempts remain in the
run and are not credited as efficiency wins. One trial per combination is an
initial comparison, not a general model ranking.

Framework session time includes model-runtime startup and cleanup. It excludes
Windows app setup and the independent saved-result check.

## Evidence and privacy

Keep the framework JSON, JUnit report, task artifacts, and app checks together
in a fresh private directory. Raw reports may contain local paths, screenshots,
model messages, or unrelated text visible in dialogs, so do not publish them.

Any future public export must contain only settings, counts, request-audit
metadata, and result hashes. It must exclude desktop images, raw window text,
model messages, authentication, and local file paths.

## Reproduce the real-app comparison

Use an unlocked Windows desktop reserved for the benchmark. Install Notepad,
Word, PowerPoint, and Chrome. Build the server and authenticate the GitHub CLI
or provide `GITHUB_TOKEN`; never put a token in source files or reports.

Run the no-model checks first:

```powershell
dotnet build src\Sbroenne.WindowsMcp -c Release
uv run --project tests\usage_evals pytest tests\usage_evals\tests\unit -q
uv run --project tests\usage_evals pytest tests\usage_evals\tests\comparison --collect-only -q
```

The live command is in
[the evaluation guide](../tests/usage_evals/README.md#controls-versus-screenshots).
It requires approval for 16 model sessions and an exclusive desktop. Without
that opt-in, the comparison is skipped.

Do not resume retired standalone runners or combine interrupted runs. Optional
AI-generated report analysis is a separate paid operation and is not part of
the comparison.

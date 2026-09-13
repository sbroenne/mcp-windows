# Windows usage evaluations

This is a fresh consumer of **pytest-skill-engineering**, separate from the older
`Sbroenne.WindowsMcp.LLM.Tests` suite. It has two equal purposes:

- Find out whether and how Windows MCP's CLI, tool descriptions, responses, and behavior
  should improve.
- Expose and fix reusable gaps in pytest-skill-engineering's evidence and feedback.

A passing test alone is not the useful result. Review what the model tried, what the product
returned, whether the documents are correct, and where recovery or confusion occurred.

## What runs

Three ordinary Notepad tasks cover creating a note, editing one without damaging other content,
and choosing the current document rather than its similarly named archive. These are new
scenarios, not copies of the old prompts. File verification does not depend on a model's claims.

Each task can run through the real **CLI** or **MCP** entry point. A fresh session and separate
working directory are created each time. No repository instructions or previous transcript are
deliberately supplied. Tool availability is configured separately from the user's task.

Sessions use an empty configuration directory and SDK-supported options to disable configuration
discovery, inherited custom instructions, on-demand instruction discovery, and file hooks. The
explicit scenario instructions and selected shell/MCP tools remain available.

**Limitations:** this is not a sandbox or complete runtime isolation. Inherited environment,
authentication, and runtime-level capabilities still exist; use a clean evaluation account.
CLI shell calls are conservatively checked for a single literal
invocation of the selected executable; unknown or alternate routes make the evaluation fail
with an explicit route-verification reason. This does not prove the absence of indirect bypasses
through desktop automation. A help lookup or recovery attempt is not automatically a product bug.
Literal quoted multiline text and PowerShell newline escapes inside double quotes are supported;
newlines between commands, command substitutions, and additional shell commands are rejected.
Keep original route verdicts when analyzing older runs rather than rewriting their evidence.

## Safe default: no model or desktop use

From this directory:

```powershell
uv run pytest tests\unit -q
uv run ruff check .
uv run pytest tests\live --collect-only -q
```

Ordinary `uv run pytest` runs the no-model checks and reports the live tests as **skipped**.
Collection neither builds the server nor opens applications. CI runs only the no-model tests.

Keep package-feed configuration at user/machine scope. `uv` does not read `pip.ini`; configure
`%APPDATA%\uv\uv.toml` if your machine requires an index proxy. Do not put internal feed URLs or
credentials in this project.

## Framework prerequisite

The project pins published framework commit
`5a533dcd149c3e27f60b0a296a047ba1d3f255ff`, including complete tool evidence,
configuration-aware reports, recoverable paid summaries, and SDK 1.0.13 support.
It is published in `sbroenne/pytest-skill-engineering#95`, stacked on SDK upgrade
`sbroenne/pytest-skill-engineering#94`; publication does not mean either PR is merged.
Older versions without `ToolCall.completion_received` and `CopilotResult.evidence_complete`
are rejected before model execution rather than silently accepting missing evidence.

For framework development only, an optional local override is:

```powershell
uv run --with-editable "$frameworkWorktree" pytest tests\unit -q
```

Set `$frameworkWorktree` to your own checkout. Do not commit an absolute local dependency path.
Normal commands below use the published pin without an override.

## Live execution requires a separate decision

First approve the model, task selection, interfaces, repeats, total number of sessions, and
per-session timeout. Any optional AI-generated report analysis is an additional model call and
needs its own approval. There is no automatic model selection or whole-session retry.

Use an **exclusive disposable interactive Windows desktop/account**, not an everyday profile:
Notepad can restore old tabs even when no process was running. Explicit opt-in and an empty
Notepad process list are required. The model is asked to leave the test document open.
Cleanup terminates only a process associated with the run's unique document title or a
successful recorded Notepad launch after the empty-desktop check, with its process creation
time checked again. This also covers an owned document whose save failed. Unidentified
processes are never terminated: cleanup rechecks their identity and waits up to five seconds
each for natural exit, so a short-lived launch helper does not cause a stale failure.
An unidentified process that remains, or a reused process ID, causes an explicit error.
These checks are not permission to use a shared desktop.

Build both entry points from the repository root:

```powershell
dotnet build src\Sbroenne.WindowsMcp.Cli -c Release
$build = (Resolve-Path src\Sbroenne.WindowsMcp.Cli\bin\Release\net10.0-windows10.0.22621.0).Path
Set-Location tests\usage_evals
```

On the approved disposable desktop, the following selects **six** model sessions: three tasks
times two interfaces. Replace the placeholders with the approved choices:

```powershell
$env:MCP_TEST_DESKTOP_INPUT = '1'
$env:MCP_USAGE_DISPOSABLE_DESKTOP = '1'
uv run pytest tests\live -v `
  --run-usage-evals --usage-model "<approved-model>" `
  --usage-max-runs 6 --usage-timeout <approved-seconds> `
  --junitxml=TestResults\usage-junit.xml `
  --usage-cli "$build\wincli.exe" --usage-server "$build\Sbroenne.WindowsMcp.exe"
```

`--usage-case create_note --usage-interface mcp` selects one session.
`--usage-repeats N` repeats each selected case/interface. The selected count must not exceed
`--usage-max-runs`. Zero selected live tests with live execution requested is an error.
The timeout bounds each session, not its token bill.

## Comparing Sol and Luna

Repeat `--usage-model` to compare models in one serial batch. For example, the following options
select **12** evaluations (three tasks, two interfaces, two models):

```powershell
--usage-model gpt-5.6-sol --usage-model gpt-5.6-luna `
--usage-reasoning-effort medium --usage-max-runs 12 --usage-timeout 300
```

Both models receive the same task families, build, timeout, and explicit reasoning setting.
Their order alternates between task/interface groups so one model is not always first.
Each has separate documents, session state, verification, and report identity. The model
reported by the runtime must match the requested model; a fallback is not a valid comparison.
Selecting baseline builds or repeats multiplies the run count further.

Keep correctness, captured evidence, and observed difficulty separate from elapsed time and
usage. An advertised price difference is not an observed quality difference or an actual bill.
With one run per task/interface/model, conclusions are exploratory, not reliability claims.

## Independent assessment, then a paid summary

To compare a human/assistant assessment with the framework's paid analysis without letting one
influence the other:

1. Run the approved live batch with `-o addopts=` and
   `--aitest-json=TestResults\sol-luna.json`. Do not pass summary, HTML, or Markdown options
   in this stage: those options can trigger paid analysis at pytest session end.
2. Inspect raw calls, returned results, independent file checks, timing, and usage. Save the
   independent assessment before requesting or reading the paid summary.
3. Generate the paid report from the saved JSON, without rerunning the evaluations:

```powershell
if (-not $env:GITHUB_TOKEN -and $env:GH_TOKEN) {
  $env:GITHUB_TOKEN = $env:GH_TOKEN
}
uv run pytest-skill-engineering-report `
  TestResults\sol-luna.json --html TestResults\sol-luna.html `
  --json TestResults\sol-luna-summary.json `
  --summary --summary-model copilot/gpt-5.6-sol --summary-attempts 3
```

4. Compare agreement, disagreement, missed findings, and unsupported claims against the same
   evidence. Keep the independent assessment out of the paid reviewer's input.

The overall summary is one generation operation, but the framework may attempt it up to
`--summary-attempts` times (1-3, default 3). This limit is per invocation, not a shared budget
across retries of the command. Reduce it for the remaining approved budget. This is additional
paid usage. Summary accounting can be unavailable; do not describe
an unknown amount as zero cost. Use the framework's tool-free, isolated judge implementation;
do not run a legacy summary path that can approve shell or desktop actions. Verify that a fresh
summary was produced, rather than treating an existing HTML file as proof of success.
The isolated judge does not use the usual CLI keychain/config login; supply the existing token
through the process environment, not by copying a user's configuration or credentials directory.

`--json` saves the evidence and successful paid analysis before rendering. If rendering fails,
reuse that checkpoint without paying for analysis again:

```powershell
uv run pytest-skill-engineering-report `
  TestResults\sol-luna-summary.json --html TestResults\sol-luna.html
```

Do not pass `--summary` when reusing saved analysis. These options are included in the pinned
framework. Review generated recommendations against the actual file checks:
pytest pass rates include harness failures, and a single sample does not justify deployment
rankings. Do not add tool hints to task prompts merely because an automated review suggests them.

Authentication accepts `GITHUB_TOKEN` or passes an existing `GH_TOKEN` to the SDK within the test
process. Otherwise it checks `gh auth status --hostname github.com`; a failing login at another
host must not silently skip this evaluation.

## Before and after a product change

Keep a separate built baseline and supply both:

```powershell
--usage-baseline-cli "$baseline\wincli.exe" `
--usage-baseline-server "$baseline\Sbroenne.WindowsMcp.exe"
```

This doubles the selected session count, so it requires a corresponding approved cap.
Each baseline/candidate combination is an independent pytest case with fresh documents and a
fresh model session. Unlike a combined assertion over two sessions, this preserves each build's
own task verdict. The framework's normal reports retain both records; `usage` properties identify
case, interface, repetition, variant, model, and executable/assembly hashes.

Compare **within an interface** first. CLI and MCP need not use the same number of calls.
One run is an observation, not a reliability claim. Review repeated samples before claiming
that a change improved performance.

## Reading the native reports

Use pytest-skill-engineering's existing JSON and optional HTML/analysis reporting, not a second
report generator. Consumer `record_property` metadata includes:

- `usage`: configuration and build identity.
- `verification`: independent file checks, including the untouched archive.
- `interface_route`: observed calls and route-verification limitations.
- `execution_evidence`: session outcome and whether the tool evidence was captured completely.
- `cleanup`: the specifically owned processes cleaned up.

A correct file, a successfully completed model session, and a complete trace are separate facts.
Keep failed and incomplete sessions: their evidence may be the most useful. Setup failures and
skips without a model result are represented by pytest/JUnit, not native evaluation records.
Retain that report as well, using a distinct JUnit filename for each batch; missing native records
must never be interpreted as successful runs.

For each improvement candidate, identify the relevant calls/results, observed difficulty,
likely cause, proposed change, and confidence. Separate product problems from framework defects,
environment failures, and weak scenarios. Do not treat the model's interpretation as fact.

Implement only evidence-backed product changes, add conventional regression tests where practical,
and request approval before follow-up model runs. Report "no justified change" or "inconclusive"
when that is what the evidence supports. Do not retire the old suite merely because this one exists.

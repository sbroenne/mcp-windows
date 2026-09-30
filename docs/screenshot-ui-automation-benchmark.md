# Screenshots versus direct field reads: four-model benchmark

**All 300 answers were correct. With every model, direct field reads used
51.4% fewer input tokens than low-detail screenshots and 70.5% fewer than
high-detail screenshots.**

This comparison measures **GPT-6 Astra, GPT-6 Luna, GPT-5.6 Sol, and GPT-5.6
Luna** reading the same Windows form. Each model receives 25 direct text reads,
25 low-detail screenshots, and 25 high-detail screenshots.

## Live AI reading results

Each row contains 25 reads. Token counts and response times below are medians
per read; seconds include network and service time.

| Model / input | Correct reads | Input tokens | Output tokens | Seconds |
|---|---:|---:|---:|---:|
| GPT-6 Astra / Direct text | 25/25 | 170 | 9 | 2.74 |
| GPT-6 Astra / Low-detail image | 25/25 | 350 | 9 | 3.21 |
| GPT-6 Astra / High-detail image | 25/25 | 576 | 9 | 3.17 |
| GPT-6 Luna / Direct text | 25/25 | 170 | 25 | 2.12 |
| GPT-6 Luna / Low-detail image | 25/25 | 350 | 10 | 2.10 |
| GPT-6 Luna / High-detail image | 25/25 | 576 | 9 | 1.99 |
| GPT-5.6 Sol / Direct text | 25/25 | 170 | 9 | 1.96 |
| GPT-5.6 Sol / Low-detail image | 25/25 | 350 | 9 | 2.41 |
| GPT-5.6 Sol / High-detail image | 25/25 | 576 | 9 | 2.39 |
| GPT-5.6 Luna / Direct text | 25/25 | 170 | 9 | 2.35 |
| GPT-5.6 Luna / Low-detail image | 25/25 | 350 | 9 | 2.59 |
| GPT-5.6 Luna / High-detail image | 25/25 | 576 | 9 | 2.91 |

### Differences between the models

**Accuracy and input usage were tied.** All four models read every value
correctly from text and both image settings. Each used the same input tokens
for each matching observation. This task does not distinguish their accuracy
on harder forms or other apps.

**Response time differed.** GPT-5.6 Sol had the lowest median time for direct
text; GPT-6 Luna had the lowest for both image settings in these runs.
Direct text was quicker than images for three models. GPT-6 Luna was slightly
quicker with images, so fewer input tokens did not always mean a quicker answer.

**GPT-6 Luna reported more output.** Across its 75 reads it used 1,277 output
tokens, including 514 reported reasoning tokens. Each other model reported
690 output tokens and zero reasoning tokens. These are service-reported counts,
not a claim about how much unreported computation each model performs.

## Sample totals

Every model completed five samples of five observations for each input type.
Each sample used 851 direct-text input tokens, 1,750 low-detail image tokens,
and 2,880 high-detail image tokens. The table totals all five samples; no
first views or unfavorable results are omitted.

| Model | Text input | Low-detail input | High-detail input | Text output | Low-detail output | High-detail output |
|---|---:|---:|---:|---:|---:|---:|
| GPT-6 Astra | 4,255 | 8,750 | 14,400 | 230 | 230 | 230 |
| GPT-6 Luna | 4,255 | 8,750 | 14,400 | 619 | 344 | 314 |
| GPT-5.6 Sol | 4,255 | 8,750 | 14,400 | 230 | 230 | 230 |
| GPT-5.6 Luna | 4,255 | 8,750 | 14,400 | 230 | 230 | 230 |

There were exactly **300 model requests**: no failed requests, retries,
incorrect answers, or malformed answers. None reported cached input or
cache writes.

## How to interpret the comparison

Input tokens are reported by the model service, including the question and
system instruction. Direct text includes both the field-discovery reply and
the value-reading reply. Savings compare the total input across all 25 reads,
including each sample's first view, rather than rounding the median first.

Output tokens include any reasoning tokens reported by the service. They are
shown separately from input, not silently added to the input-saving claim.
Token counts are not currency costs: models and services can charge differently.

Response time runs from sending the observation to receiving the completed
answer. It includes network and service time, but excludes session creation
and Windows capture time. Requests run one at a time; models run in the order
Astra, GPT-6 Luna, Sol, GPT-5.6 Luna. Input types rotate within each model.
Times describe these batches, not a guaranteed model-speed ranking: service
load and the time of each batch can affect them.

## Method

### The question and expected answers

Every read starts a fresh session with no tools, skills, repository
instructions, or previous answers. The question is:

> What is the username entered in this form? Reply with JSON only:
> {"username": "the exact value"}.

The system instruction is identical for every model and input type:
`Answer the user's question using only the supplied observation.`
The text route appends both saved JSON replies under `Observed form controls:`.
The image routes attach the original JPEG as `form.jpg`, without the answer
in the filename or prompt. Expected values stay in the scorer.

The five values are `Baseline`, `Alice`, `Alice Smith`, `Bob`, and `Carol`.
There are five captures of each value: 25 images of one form, not 25 different
apps or forms. Images are 700 by 550 pixels. The form also repeats the username
in a status heading; the image route can read either visible occurrence.
Scoring requires an exact string match in the requested JSON format.

### Images, controls, and model settings

The runner sets the actual Responses API `input_image.detail` field to `low`
or `high` through the Copilot SDK's supported request-handler hook. It checks
that the submitted bytes match the saved screenshot's SHA-256 hash. It does
not simulate low detail by resizing a file.

All four models use **medium reasoning effort**, the same question,
instructions, and saved inputs. The comparison checks outgoing model IDs,
image detail, reported response models, usage, and input hashes. Models are
called explicitly, not selected through automatic routing.

The saved images come from Windows MCP's production `ScreenshotService`:
window-only JPEG, quality 60, without cursor or annotations. The full control
tree is read before and after each screenshot. Both reads must succeed, be
nonempty, contain no truncation warning, and match. This checks control-state
stability, not purely visual animations.

Direct reads discover the unique `UsernameInput` edit control and read its
value. Both replies are included every time. Choosing this selector is outside
the reading test, just as deciding where to inspect an image is.

### Scope

This compares reading a field from an observation. It does not include
choosing tools, navigating the app, entering values, or completing a whole
user task. It is not a comparison of separate products.
A full-window control list is not a focused field read
and can contain much more information.

### Run evidence

The live model requests ran on **2026-09-27** through GitHub Copilot, using
Copilot SDK **1.0.14** and runtime **1.0.85**.

The [complete machine-readable evidence](../gh-pages/docs/assets/benchmarks/screenshot-readability.json)
contains every answer, expected value, outgoing detail setting, usage count,
duration, and image/text hash, plus a checked summary for each model. Each
run records its source revision, dirty working-tree state, and SHA-256 of the
exact runner. Wrong and malformed answers are retained. Transport failures
stop the runner and preserve the incomplete trial rather than silently retrying.

The source captures are from
`screenshot-winforms-field-editing-9a1e0081953948b1ac39f960ac0932bc`,
recorded on Windows 10.0.26220.0 and .NET 10.0.12, Release build with SDK
10.0.401. Source was an uncommitted working tree based on
`67ba78d6b3a99bdabee27898dab64b38aafb55ed`, including the benchmark and the
branch's automation changes, not a released package.

| Capture binary | SHA-256 |
|---|---|
| `Sbroenne.WindowsMcp.Tests.dll` | `CB9103F1EB18154FC8A783150A7AE55D3695E592E0854FB37005DFA272D4E0A2` |
| `Sbroenne.WindowsMcp.dll` | `5DD7C47215AA1B495CCFF4A46414B6D300A9A1AF88958E8A8E3C2272069614FA` |

Raw screenshots and response JSON remain local; the commands below produce
fresh captures. Control identifiers can vary between capture runs, so text
counts can vary slightly. All four model runs here use the same saved captures.

## Reproduce

### Capture the form

Use an unlocked Windows desktop and the test prerequisites in
[the testing instructions](../.github/testing.instructions.md).
Do not use the desktop while this capture runs. These commands do not make
paid model requests.

```powershell
$env:MCP_SNAPSHOT_BENCHMARK_OUTPUT = Join-Path $env:TEMP "windows-mcp-screenshot-benchmark"
dotnet build tests\Sbroenne.WindowsMcp.Tests -c Release
dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release --no-build --filter "FullyQualifiedName~Unit.ScreenshotComparisonBenchmarkTests|FullyQualifiedName~Unit.SnapshotBenchmarkRunnerTests"
dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release --no-build --filter "FullyQualifiedName~Benchmark_ScreenshotVersusControls_WinForms" --logger "console;verbosity=detailed"
```

A completed capture writes `report.md`, `observations.json`, and a JPEG plus
response JSON for each observation. Use its uniquely named folder below.
The live comparison uses actual model usage, not the capture report's size
estimates.

### Model reads

The published 300 reads remain historical evidence. Their standalone SDK runner
is retired; the retained Python helpers only read and validate saved evidence.
Do not use that script to start more paid requests.

New model-driven comparisons must run through **pytest-skill-engineering**.
The current live suite measures [complete real-app tasks](real-app-benchmark.md),
not another set of single-field reads. Reintroducing live single-field reads
requires a framework-based test and separate approval of its model-call budget.
The summary command rejects incomplete runs, duplicate cases, mismatched
inputs/settings, and inconsistent usage or scoring.

Offline checks make no model requests:

```powershell
python -m unittest discover -s scripts\tests -p "*readability*.py" -v
python -m unittest discover -s gh-pages\tests -v
```

The [changes-only measurement](incremental-snapshot-benchmark.md) compares
text updates with full text. It measures a separate capability, not another
saving to add to the screenshot comparison.

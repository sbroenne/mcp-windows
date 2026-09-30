---
title: Token measurements
description: See the method for comparing controls and screenshots, plus measured single-field and changes-only results.
hide:
  - toc
---

# Token measurements

**Read just what you need. Send only what changed.** Windows MCP gives your
assistant both ways to save tokens, with screenshots available when it needs
a picture.

Tokens are the units AI services count when processing text and images.
The real-task comparison covers full tasks. Smaller measurements below cover
reading a single field and sending only changes instead of repeating a
window's controls.

## Real tasks in Notepad, Word, PowerPoint, and Chrome

The 16-trial run compares GPT-6.1 Sol and GPT-6 Luna on Notepad, Word,
PowerPoint, and Chrome. Controls first completed **8 of 8** tasks. Screenshots
only completed **7 of 8**.

Across the seven app/model pairs where both routes completed, controls first
used a median **34.9% fewer input tokens** and took a median **34.3% less
framework session time**. The detailed table below includes the failed trial
and the pair where controls used more.

Saved files and submitted values are checked independently. The framework
also checks actual model requests, tools, reasoning, image detail, usage, and
stop reasons before a result can be published.

<details markdown="1" id="real-app-details">
<summary>Real-app tasks, safeguards, and how to run the comparison</summary>

--8<-- "_generated/real-app-benchmark.md"

</details>

<span id="less-repeated-information"></span>

## Screenshots versus direct reads

**The AI read the field correctly every time. Direct reads used 51.4% fewer
input tokens than low-detail screenshots and 70.5% fewer than high-detail
screenshots.**

We gave **GPT-6 Astra, GPT-6 Luna, GPT-5.6 Sol, and GPT-5.6 Luna** the same
25 saved form views in three ways: field text, low-detail screenshots, and
high-detail screenshots. **All 300 answers matched the entered value exactly.**
Each read started fresh, without previous answers or tools that could look
up the answer.

--8<-- "assets/charts/screenshot-form.html"

The chart uses actual input-token counts reported by the model service,
including the question and instructions. Savings use the totals from all
25 reads per input type, including the first view.

**Screenshots worked too.** Every model returned 25 correct answers out of 25
at both image settings. Focused reads returned those same answers with less input. That is
the point: send what the task needs, with a picture available when useful.

The test used five values, captured five times each in one form. It measures
reading the field, not completing an entire task or comparing separate products.

## What differed in the single-field test?

**Accuracy and input tokens were the same.** Every model used a middle
input count of **170 tokens for direct text, 350 for a low-detail image, and
576 for a high-detail image**. None was more accurate on this form.

**Response times differed.** The table shows the middle time in seconds
across 25 reads for each model and input type.

| Model | Direct text | Low-detail image | High-detail image |
|---|---:|---:|---:|
| GPT-6 Astra | 2.74 | 3.21 | 3.17 |
| GPT-6 Luna | 2.12 | 2.10 | 1.99 |
| GPT-5.6 Sol | 1.96 | 2.41 | 2.39 |
| GPT-5.6 Luna | 2.35 | 2.59 | 2.91 |

GPT-5.6 Sol was quickest for text reads; GPT-6 Luna was quickest for images
in this run. Smaller input did not always mean a quicker answer. These times
include network and service delays, and the models ran in separate batches,
so they are observations rather than a guaranteed speed ranking.

**GPT-6 Luna reported more output tokens:** 1,277 over its 75 reads, compared
with 690 for each other model. That includes 514 reported reasoning tokens
for GPT-6 Luna. Input savings and output use are different measurements.

All four handled this form successfully. The useful difference here was
how much they returned and how long they took, not whether they could read it.

<details markdown="1" id="screenshot-comparison-details">
<summary>All four models: detailed results, recorded answers, and how to repeat the tests</summary>

--8<-- "_generated/screenshot-benchmark.md"

</details>

## Changes-only text versus full text

**84.0% to 96.6% fewer approximate tokens in changes-only text updates.**
That is the range measured in our Excel, Word, and desktop test-app workloads,
compared with returning the same controls in full after each action.

Unlike the screenshot comparison above, these figures **exclude the first
view** and use a different text-counting method. They measure repeated text
updates, not savings against screenshots. Do not add the two benchmarks'
percentages together or with the real-task results.

--8<-- "assets/charts/text-updates.html"

Windows MCP sends a full view when a changes-only reply would be unsafe or
too large. Smaller updates are useful, but they are not guaranteed at every
step. The detailed measurements are expandable below.

<span id="why-that-helps"></span>
<span id="fewer-tokens-when-a-picture-is-not-needed"></span>

## Put the results to use

Ask your assistant to read the field, checkbox, or table needed for the task,
rather than the whole window. For repeated checks, it can request only changes.
Use screenshots when appearance matters or an app does not provide useful
controls.

You do not need to learn tool settings for normal use. Your assistant chooses
which view to request.

## Does it make tasks faster?

The real-task comparison measures full framework session time for both routes.
Its seven completed pairs had a median 34.3% reduction with controls first,
but one PowerPoint pair took 35.0% more time. The single-field test did not
show a consistent speed advantage. An app's speed and the time the AI takes to
decide also affect the task.

<span id="do-i-need-to-configure-this"></span>

For scripts and tool settings, use the
[command reference](https://github.com/sbroenne/mcp-windows/blob/main/FEATURES.md).

[Compare controls and screenshots](comparison.md)

<details markdown="1" id="text-update-details">
<summary>Changes-only text benchmark: method and individual samples</summary>

--8<-- "_generated/benchmark.md"

</details>

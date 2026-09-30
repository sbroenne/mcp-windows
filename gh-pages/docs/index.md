---
template: home.html
title: Home
description: Put your AI to work in Windows with direct controls and screenshots. Read the measured results and their limits.
hide:
  - navigation
  - toc
---

**Your assistant. Your Windows apps. One set of tools.**

Use Windows MCP with GitHub Copilot, Claude, Cursor, or another compatible
assistant. Connect through **MCP or the command line (CLI)**. Keep the AI you
prefer and give it the tools to act in Windows. Both routes work with AI agents:
an agent with command access can run the CLI for you.

[Set up your assistant](installation.md){ .md-button .md-button--primary }

<span id="why-controls-not-just-screenshots"></span>
<span id="less-information-to-repeat"></span>
<span id="fewer-tokens-when-a-picture-is-not-needed"></span>

## Why choose Windows MCP?

<div class="grid cards" markdown>

-   **Save tokens**

    ---

    Read the answer, not a picture of the answer. Get a field's value or a
    checkbox's state as text, then request only changes instead of repeating
    the whole window.

-   **Target real controls**

    ---

    Find the Save button and act on that particular button, rather than work
    out where to click in a screenshot. Read text, checked states, and table
    rows directly.

-   **Keep screenshots as a fallback**

    ---

    Both approaches are built in. When appearance matters or controls are not
    available, return a screenshot for the AI to inspect and use mouse and
    keyboard actions.

-   **Use your preferred assistant**

    ---

    Connect through MCP, a standard way to give an assistant tools, or the
    command line (CLI). Use the same Windows capabilities with Copilot, Claude,
    Cursor, and other compatible assistants.

</div>

<span id="measured-token-savings"></span>

## Real tasks: controls first versus screenshots

In 16 trials with GPT-6.1 Sol and GPT-6 Luna, controls first completed **8 of
8** tasks. Screenshots only completed **7 of 8**. Across the seven matching
pairs where both routes completed, controls first used a median **34.9% fewer
input tokens** and took a median **34.3% less framework session time**.

We check saved files and submitted values independently. The original input
must remain unchanged, and incomplete request evidence stops the run. Controls
did not win every pair: see the full table and limits before using the median.

[See the tasks, safeguards, and full method](benchmark.md#real-tasks-in-notepad-word-powerpoint-and-chrome)

## How Windows MCP saves tokens

AI services count the text and images they read in units called **tokens**.
Windows MCP can reduce what your assistant has to read in two ways.

### Read one field instead of the whole window

Need a name from a form? Read that field, rather than send every button, field,
and menu in the window. We asked GPT-6 Astra, GPT-6 Luna, GPT-5.6 Sol, and
GPT-5.6 Luna to read the same field from text and screenshots.
**All 300 answers were correct. Direct reads used fewer tokens with every model.**

--8<-- "assets/charts/screenshot-form.html"

**Get the value directly, not from a picture.** Your assistant receives the
text it needs for the next step, without interpreting the rest of the window.
Screenshots also worked at both low and high detail in this form test, so
your assistant can use a picture when it needs one.

[See the full screenshot comparison](benchmark.md#screenshots-versus-direct-reads)

### Send changes instead of repeating everything

After a value changes, your assistant can receive just the changes instead
of another full description of the window. These tests show how much that
reduced the text sent to the AI.

--8<-- "assets/charts/text-updates.html"

This second chart compares text with text, not screenshots. The percentages
are not extra savings to add to the first chart.

These two smaller tests measure information sent, not a complete task or a
dollar bill.

[See all results and how we measured them](benchmark.md)

## Start with a small task

After setup, open Notepad and ask your assistant:

> Type "Hello from Windows MCP" in Notepad and save it as a new file named
> windows-mcp-demo.txt on my Desktop.

Use a new test file, not an important document. Your assistant may ask for
permission before it acts.

## What can it help with?

| You want to... | Try asking... |
|----------------|---------------|
| Understand a message | "Read the error in this window and explain it." |
| Fill in a form | "Fill in this form with these details. Stop before submitting it." |
| Read a table | "Read this table and list the rows with missing values." |
| Save your work | "Save this document as a new file." |
| Arrange your windows | "Move Notepad to my second monitor." |

[More examples and limits](features.md)

## How it works

**Your assistant makes the decisions. Windows MCP carries out the Windows
actions and returns the results.** It runs locally on your PC.

<figure class="mcp-flow">
  <div class="mcp-flow__node">
    <strong>Your AI assistant or script</strong>
    <span>MCP or command line (CLI)</span>
  </div>
  <div class="mcp-flow__arrow" aria-hidden="true">&#8597;</div>
  <div class="mcp-flow__node">
    <strong>Windows MCP</strong>
    <span>Runs on your PC</span>
  </div>
  <div class="mcp-flow__arrow" aria-hidden="true">&#8597;</div>
  <div class="mcp-flow__branches">
    <div class="mcp-flow__node">
      <strong>Windows UI Automation</strong>
      <span>Read and use actual controls</span>
    </div>
    <div class="mcp-flow__node">
      <strong>Screenshots, mouse and keyboard</strong>
      <span>For visual tasks</span>
    </div>
  </div>
  <div class="mcp-flow__arrow" aria-hidden="true">&#8597;</div>
  <div class="mcp-flow__node"><strong>Your Windows apps</strong></div>
  <figcaption>Requests go to Windows MCP. Results return to your assistant.</figcaption>
</figure>

**Connect through MCP or CLI.** An assistant can call MCP tools, or a coding
agent can run `wincli` through its existing terminal tool on your Windows PC.
You ask for the task; the agent chooses the calls or commands. Both routes
use the same Windows automation code. The CLI also works in scripts.

**Read and act on controls.** Windows MCP uses **Windows UI Automation**,
Windows' system for exposing buttons, fields, tables, and their values.
It gives the assistant a reference to each control it finds. The assistant
can then target that control, rather than work out where to click in a picture.

**Return the information needed.** Read a field as text, or request a view of
the window. For repeated views, Windows MCP can return only changes when that
is safe and smaller. When appearance matters or controls are not available,
the assistant can request a screenshot and use mouse and keyboard actions.

The assistant should check what happened after an action. A successful click
does not, by itself, prove that a file was saved or a form was submitted.

[More about how it works](architecture.md)

## When should your assistant use controls or screenshots?

**Read controls when they provide the answer. Use screenshots when appearance
matters.** Windows MCP provides both approaches.

Focused text reads and changes-only updates can reduce repeated information.
Screenshots remain useful when an app does not expose the controls or values
needed for the task.

[Compare controls and screenshots](comparison.md)

## Before you begin

Windows MCP uses your real desktop, including apps where you are already signed
in. Keep private information out of view unless it is needed for the task.
Review your AI app's permission settings, and leave the mouse and keyboard
alone while the assistant is working.

Screenshot-based use needs an AI that understands images and a client that
passes those images to it.

[Use it safely](security.md) | [Understand what data is shared](privacy.md)

---
name: "browser-discoverability"
description: "Keep browser automation discovery compact by extending existing semantic UI automation guidance"
domain: "prompt-design"
confidence: "high"
source: "earned"
---

## Context

Use this when browser support already exists in the backend and the goal is to help users or LLMs discover it without adding a separate automation model.

## Patterns

### Reuse the default semantic workflow

Add a short browser note to the main quickstart or primary guidance surface instead of building a parallel browser-only story.

### Add one focused browser prompt

Create a single prompt that covers the browser-specific gaps: launching with a URL, using ARIA labels as names, and using shortcuts for browser chrome like the address bar or tab switching.

### Keep tool descriptions example-driven

Prefer tiny examples in `app`, `ui_find`, and `ui_click` over long prose. Mention `msedge.exe` or `chrome.exe`, visible text, and ARIA labels.

### Keep docs thin

Use one short README note and one compact reference section when documentation is needed. Avoid repeating the same browser guidance across every doc surface.

## Examples

- `WindowsAutomationPrompts.Quickstart()` gets one browser sentence.
- `WindowsAutomationPrompts.BrowserAutomation()` handles URL launch, ARIA discovery, and keyboard fallback for browser chrome.
- Tool descriptions mention `msedge.exe` launch and ARIA-backed `name` matching.

## Anti-Patterns

- Duplicating full browser workflows across many prompts, resources, and docs.
- Introducing Playwright/Selenium/CDP language when the product direction is semantic UI Automation first.
- Explaining browser chrome and page-content automation as separate systems when they are mostly the same workflow plus a few shortcuts.

---
name: "prompt-budget-guardrails"
description: "How to test prompt guidance for usefulness without letting prompt size or claims sprawl"
domain: "testing"
confidence: "high"
source: "earned"
---

## Context
Use this when changing prompt templates, prompt-facing docs, or MCP prompt discovery behavior. It is especially useful when the product wants to ship new guidance quickly but QA needs to keep scope honest and token usage under control.

## Patterns
- Pair **content assertions** with **budget assertions**. Verify the prompt says the right thing (for example, "best-effort" or "use metadata before pixels") and also verify it stays smaller than a broader baseline prompt.
- Prefer **relative token budgets** over fake absolutes for large canonical prompts. A browser-specific prompt should usually be smaller than the generic quickstart, even if the quickstart itself is not tiny.
- Add **prompt discovery integration** so the server actually exposes the prompt you just tested at unit level.
- For browser-adjacent validation, use the existing Electron/Chromium harness to test ARIA-label and search/navigation patterns safely before claiming real-browser parity.

## Examples
- `tests/Sbroenne.WindowsMcp.Tests/Unit/Prompts/WindowsAutomationPromptsTests.cs`
- `tests/Sbroenne.WindowsMcp.Tests/Integration/PromptDiscoveryTests.cs`
- `tests/Sbroenne.WindowsMcp.Tests/Integration/ElectronHarness/UIAutomationElectronTests.cs`

## Anti-Patterns
- Do not call a prompt "token-efficient" without a budget-oriented assertion.
- Do not upgrade docs from Electron/Chromium confidence to full browser support without real browser coverage.
- Do not use tool-hint-heavy prompt tests; keep them focused on the task and the guidance being shipped.

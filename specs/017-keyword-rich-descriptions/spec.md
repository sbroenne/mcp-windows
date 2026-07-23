# Feature Specification: Keyword-Rich Tool Descriptions

**Feature Branch**: `stbrnner-microsoft-adopt-windows-mcp-ideas`  
**Created**: 2026-07-22  
**Status**: Implemented  
**Input**: Adopted from a comparative review of [CursorTouch/Windows-MCP](https://github.com/CursorTouch/Windows-MCP), whose tool descriptions embed rich natural-language keyword lists. Standardize a `Keywords:` line in every tool description so an LLM can reliably match a user's task phrasing to the right tool.

---

## Overview

An MCP client selects a tool purely from its `name` and `description`. When a user says "close the frozen app", "copy this text out", or "what does the screen say", the model must map that phrasing onto a tool. A synonym-rich `Keywords:` line in each description widens that match surface without changing behaviour.

Every tool's `<summary>` (the source of its `[Description]` via the XML-to-description source generator) ends with a single line of the form:

```
Keywords: <comma-separated synonyms and task phrasings>
```

---

## Functional Requirements

- **FR-1**: Every MCP tool description MUST contain a `Keywords:` line.
- **FR-2**: Keyword lists SHOULD include user-facing synonyms and task phrasings (verbs and nouns a person would use), not internal parameter names.
- **FR-3**: The `Keywords:` line MUST live in the tool method's XML `<summary>` so it flows through the existing `XmlToDescriptionGenerator` into the MCP `tools/list` description — no separate metadata channel.
- **FR-4**: A unit test MUST enforce FR-1 across the whole catalog, so any newly added tool fails the build until it includes keywords.

## Non-Goals

- Changing tool names, parameters, or behaviour.
- A structured/queryable keyword index — keywords are prose within the description, consumed by the model as-is.
- Localization of keywords.

---

## Test Coverage

- **Unit** (`ToolCatalogTests.GetTools_EveryDescriptionContainsKeywordsLine`): asserts every entry returned by `ToolCatalog.GetTools()` has a description containing `Keywords:`. Because the catalog is auto-discovered from the assembly, this automatically covers future tools.

## Notes

- The catalog remains the single source of truth; the test derives from it rather than a hand-maintained list.
- `process` (spec 016) shipped with its `Keywords:` line from the start; this feature backfilled the other 18 tools.

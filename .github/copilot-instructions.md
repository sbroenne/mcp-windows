# Copilot Instructions for mcp-windows

## Sister Projects

`mcp-windows` is one of a family of Windows-only MCP-server repos maintained by the same author
(`sbroenne`). The family also includes:

- **`mcp-server-excel`** (`sbroenne/mcp-server-excel`) — Excel COM automation. This is the
  **authoritative architectural template** for the family's general server-side patterns: layered
  architecture (Core → Service → CLI/MCP Server), the "two equal entry points" (MCP Server + CLI)
  principle, Unified Service Architecture (shared daemon/session registry), and its support/audit
  scripts (`scripts/*.ps1`) and CI Gate workflow. `mcp-windows` is UI-automation-focused (FlaUI/
  pywinauto patterns) rather than COM/Office-interop-focused, so not every Excel pattern applies
  directly — but when solving a cross-cutting problem (session lifecycle, CLI/MCP parity, daemon
  process management, pre-commit audit tooling), check how Excel solved it first.
- **`mcp-server-powerpoint`** (`sbroenne/mcp-server-powerpoint`) — PowerPoint COM automation,
  structurally mirrors `mcp-server-excel`.

If you fix a real bug or process/tooling gap here that likely also exists in the other repos,
flag it so the same fix can be considered there.

## Core Principles

The project follows these NON-NEGOTIABLE principles:
- **Test-First Development** - Write tests before implementation
- **MCP Protocol Compliance** - Follow MCP specification strictly
- **Augmentation, Not Duplication** - Tools are "dumb actuators", no complex logic in tools
- **Microsoft Libraries First** - Prefer official Microsoft libraries over third-party alternatives
- **Security Best Practices** - No secrets in code, validate inputs, follow security guidelines
- **Modern .NET & C#** - Use current .NET and C# language features
- **xUnit Testing** - All .NET tests use xUnit framework
- **LLM Integration Testing** - Use pytest-skill-engineering with GitHub Copilot SDK
- **Token Optimization** - Optimize MCP responses for LLM token efficiency
- **UI Automation First** - Semantic UI automation is the primary approach

## Automation Patterns Reference

**ALWAYS consult FlaUI and pywinauto when adding automation code or debugging issues.**

These are the reference implementations for Windows UI automation patterns:

### Reference Repositories
- **FlaUI**: https://github.com/FlaUI/FlaUI - C# UI Automation library
- **pywinauto**: https://github.com/pywinauto/pywinauto - Python Windows automation

### Key Patterns to Reference

| Pattern | FlaUI Reference | pywinauto Reference |
|---------|-----------------|---------------------|
| Modal windows | `window.ModalWindows` with `Retry.WhileEmpty` (1s timeout) | `app.Dialog` / `app.SaveAs` as separate windows |
| Save dialogs | `UtilityMethods.CloseWindowWithDontSave` | `app.SaveAs.Edit.set_edit_text()` + `.Save.click()` |
| Wait patterns | `Wait.UntilInputIsProcessed()`, `Retry.While*` | `.wait('ready')`, `Timings.window_find_timeout` |
| Click fallback | `element.Click()` → `Mouse.Click(element.GetClickablePoint())` | `ctrl.click_input()` → `ctrl.click()` |

### When to Check These Repos

1. **Adding new UI automation features**: Search for similar functionality first
2. **Debugging automation failures**: Check how they handle edge cases
3. **Handling modal dialogs**: Always use FlaUI's ModalWindows pattern
4. **Save/Open dialogs**: Reference pywinauto's dialog handling
5. **Timing/wait issues**: Check their retry and wait strategies

## Testing

**See [testing.instructions.md](.github/testing.instructions.md) for complete testing guidance.**

Quick reference:
- **Unit tests**: `dotnet test --filter "FullyQualifiedName~Unit"`
- **Integration tests**: `dotnet test --filter "FullyQualifiedName~Integration"` (ALL MUST PASS)
- **LLM tests**: `cd tests/Sbroenne.WindowsMcp.LLM.Tests && uv run pytest -v` (expensive - only when requested)

## LLM Test Authoring (CRITICAL)

**NEVER put tool hints in LLM test prompts. This defeats the entire purpose of the test.**

LLM test prompts must be **task-focused, not tool-focused**:

| ❌ WRONG (tool hints) | ✅ CORRECT (task-focused) |
|----------------------|---------------------------|
| "Use App-Tool to launch Notepad" | "Create a text file" |
| "Call State-Tool to get coordinates" | "Check Windows Update" |
| "Use ui_click with nameContains='Save'" | "Save the document" |

**The test evaluates whether the LLM can discover the right tools from their descriptions.**

If a test fails because the LLM can't figure out which tools to use:
1. ✅ Improve the **tool descriptions** in the MCP server
2. ✅ Improve the **system prompts** (WindowsAutomationPrompts.cs)
3. ❌ **NEVER** add hints to test prompts - that's cheating

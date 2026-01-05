# Copilot Instructions for mcp-windows

## Project Constitution

**ALWAYS read [.specify/memory/constitution.md](.specify/memory/constitution.md) for authoritative project principles.**

The constitution is NON-NEGOTIABLE and covers:
- Test-First Development (Principle I)
- MCP Protocol Compliance (Principle III)
- Augmentation, Not Duplication (Principle VI) - tools are "dumb actuators"
- Microsoft Libraries First (Principle VII)
- Security Best Practices (Principle VIII)
- Modern .NET & C# (Principle XIII)
- xUnit Testing (Principle XIV)
- LLM Integration Testing with agent-benchmark (Principle XXIII)
- Token Optimization for LLM Efficiency (Principle XXIV)
- UI Automation First (Principle XXV) - semantic UI automation is primary approach
- And more (25 principles total)...

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
- **LLM tests**: Use `Run-LLMTests.ps1` script (expensive - only when requested)

# 🪟 Windows MCP Server

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](#)

**Windows automation that actually works.** Uses the Windows UI Automation API to find buttons by name, not pixels. Tested with real AI models before every release.

## Why This Exists

Screenshot-based automation doesn't work reliably. Vision models guess wrong, coordinates break when windows move or DPI changes, and you burn through thousands of tokens on retry loops. We tried it (check the commit history) — it failed too often to be useful.

Windows MCP Server asks Windows directly: "What buttons exist in this window?" Windows knows. It's deterministic.

## How It Works

```
# 1. Find the window
window_management(action='find', title='Notepad') → handle='123456'

# 2. Click elements by name
ui_click(windowHandle='123456', nameContains='Save')

# 3. Type into fields
ui_type(windowHandle='123456', controlType='Edit', text='Hello World')

# 4. Fallback for games/canvas — screenshot + mouse
screenshot_control(windowHandle='123456') → element coordinates
mouse_control(action='click', x=450, y=300)
```

Same command works every time. Any machine. Any DPI. Any theme.

## Key Features

- **🧠 Semantic UI** — Find elements by name, not coordinates. Works regardless of DPI, theme, or window position.
- **� Multi-Monitor** — Full support for multiple displays with per-monitor DPI scaling.
- **🧪 LLM-Tested** — 54 tests with real AI models (GPT-4.1, GPT-5.2). 100% pass rate required for release.
- **💻 Broad App Support** — Tested against classic Windows apps, modern Windows 11 apps, and Electron apps (VS Code, Teams, Slack).
- **🔄 Full Fallback** — Screenshot + mouse + keyboard for games and custom controls.
- **🪙 Token Optimized** — Short property names, JPEG screenshots, auto-scaling. ~60% fewer tokens than standard JSON.

## Installation

**VS Code Extension** — [Install from Marketplace](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp). Works with GitHub Copilot automatically.

**Standalone** — [Download from Releases](https://github.com/sbroenne/mcp-windows/releases). Add to your MCP config:

```json
{ "servers": { "windows": { "command": "path/to/Sbroenne.WindowsMcp.exe" } } }
```

## Tools

| Tool | Purpose |
|------|---------|
| `ui_click` | Click buttons, checkboxes, menu items by name |
| `ui_type` | Type into text fields |
| `ui_find` | Discover elements in a window (with timeout/retry) |
| `ui_read` | Read text from elements (with OCR fallback) |
| `file_save` | Save files via Save As dialog |
| `screenshot_control` | Get element metadata (image optional) |
| `window_management` | Find, activate, move, resize windows |
| `mouse_control` | Coordinate-based clicks (fallback for games) |
| `keyboard_control` | Hotkeys and key sequences |
| `app` | Launch applications |

Full reference: [FEATURES.md](FEATURES.md)

## ⚠️ Caution

This MCP server controls your Windows desktop. Use responsibly.

## Testing

```bash
dotnet test                                      # All tests
dotnet test --filter "FullyQualifiedName~Unit"   # Unit only
```

**Framework coverage**: Tests run against WinForms, WinUI 3, and Electron apps.

**LLM tests**: 54 tests with real AI models (GPT-4.1, GPT-5.2). 100% pass rate required for release.

```powershell
cd tests/Sbroenne.WindowsMcp.LLM.Tests
.\Run-LLMTests.ps1 
```

Requires Azure OpenAI access. See [LLM Tests README](tests/Sbroenne.WindowsMcp.LLM.Tests/README.md).

## Related Projects

- **[agent-benchmark](https://github.com/mykhaliev/agent-benchmark)** — LLM agent testing framework (powers our integration tests)
- **[Excel MCP Server](https://excelmcpserver.dev)** — AI-powered Excel automation
- **[OBS Studio MCP Server](https://github.com/sbroenne/mcp-server-obs)** — AI-powered streaming control

## Documentation

| Document | Description |
|----------|-------------|
| [FEATURES.md](FEATURES.md) | Complete tool reference — all actions, parameters, examples |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Build instructions, coding guidelines, PR process |
| [LLM Tests README](tests/Sbroenne.WindowsMcp.LLM.Tests/README.md) | How to run LLM integration tests |
| [Release Setup](.github/RELEASE_SETUP.md) | Azure OIDC and GitHub Actions configuration |

## License

MIT — see [LICENSE](LICENSE)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md)

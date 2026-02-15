# Documentation Guidelines

This document defines the purpose and content rules for each documentation file to avoid duplication and ensure each serves its intended audience.

## Core Message (All Documents)

> Screenshot-based Windows automation doesn't work reliably. Vision models guess wrong, coordinates break when anything changes, and you burn through thousands of tokens on retry loops.
>
> Windows MCP Server uses the Windows UI Automation API — the same API screen readers use. It asks Windows directly: "What buttons exist?" Deterministic. Same command works every time.
>
> Tested with real AI models (GPT-4.1, GPT-5.2) before every release. 54 tests, 100% pass rate required.

## Three Entry Points

Each document is indexed by search engines and must work standalone.

| Document | Audience | Discovery Path | Length |
|----------|----------|----------------|--------|
| `README.md` | Developers | GitHub search, repo browsing | ~80 lines |
| `gh-pages/index.md` | General users | Google, windowsmcpserver.dev | ~130 lines |
| `vscode-extension/README.md` | VS Code users | VS Code Marketplace | ~60 lines |

## Content Rules

### GitHub README (`README.md`)

**Audience**: Developers evaluating the project

**Tone**: Technical, concise, honest

**Structure**:
1. Hero: "Windows automation that actually works"
2. Why This Exists: 3 sentences on why screenshot approach fails
3. Quick Example: `window_management` → `ui_click` workflow
4. Install: VS Code (1 line) + Download (1 line)
5. Tools: 2-column table (tool, purpose)
6. Caution
7. Testing: LLM tests + framework coverage
8. Related Projects: pytest-aitest, Excel MCP, OBS MCP
9. License / Contributing (one line each)

**Must include**:
- Code examples using correct tool names (`ui_click`, `ui_type`, etc.)
- Tools table
- Framework coverage (WinForms, WinUI 3, Electron)
- Link to pytest-aitest
- Links to FEATURES.md for details

**Must NOT include**:
- Comparison tables (move to gh-pages for SEO)
- Feature bullet lists (link to FEATURES.md)
- Natural language examples ("Ask your AI to...")

### Website (`gh-pages/index.md`)

**Audience**: Users discovering via search

**Tone**: Persuasive, clear, SEO-focused

**Structure**:
1. Hero + badges
2. The Problem: Why screenshot automation fails (expanded)
3. How It Works: UI Automation API + comparison table
4. Tested with Real AI: Prominent, link to pytest-aitest
5. What You Can Do: Natural language examples
6. Quick Start: Both install options
7. Tools: Full table
8. Feature Cards: 4 max (Semantic UI, LLM-Tested, Framework Coverage, Fallback)
9. Caution
10. Related Projects: pytest-aitest, Excel MCP, OBS MCP
11. Footer

**Must include**:
- Comparison table (reliability focus, "thousands of tokens")
- Natural language examples
- Feature cards (max 4)
- Link to pytest-aitest
- SEO keywords in frontmatter

**Must NOT include**:
- Code examples (tool call syntax)
- "Who Uses This" section (generic, no value)
- More than 4 feature cards

### VS Code Extension README (`vscode-extension/README.md`)

**Audience**: VS Code users ready to install

**Tone**: Copilot-focused, simple, practical

**Structure**:
1. Hero: "Let GitHub Copilot control Windows"
2. What Can Copilot Do: 5 example commands
3. How It Works: 2 sentences
4. Requirements: Windows 10/11, .NET 10 (auto-installed)
5. Caution
6. Links: docs, GitHub, issues

**Must include**:
- Copilot-specific language
- Natural language examples
- Requirements

**Must NOT include**:
- Comparison tables
- Feature bullet lists
- Code examples
- pytest-aitest link (not relevant to end users)

## Key Differentiators

Ordered by importance:

1. **Reliability over speed** — Screenshot approach doesn't work, UI Automation does
2. **LLM-tested** — 54 tests with real AI models, 100% pass required
3. **Framework coverage** — Tested against WinForms, WinUI 3, and Electron
4. **Token efficiency** — Text responses, not images (thousands of tokens saved)
5. **Full fallback** — Screenshots + mouse/keyboard when accessibility unavailable
6. **Focused** — No duplicate terminal/file tools

## Correct Tool Names

Always use the actual tool names:

| ✅ Correct | ❌ Wrong |
|-----------|----------|
| `ui_click(windowHandle='...', nameContains='Save')` | `ui_automation(action='click'...)` |
| `ui_type(windowHandle='...', text='...')` | `ui_automation(action='type'...)` |
| `ui_read(windowHandle='...', ...)` | `ui_automation(action='ocr'...)` |
| `ui_find`, `file_save` | `ui_automation(action='...')` |

## Related Projects

**README.md and gh-pages only:**
- [pytest-aitest](https://github.com/sbroenne/pytest-aitest) — LLM testing framework
- [Excel MCP Server](https://excelmcpserver.dev) — Excel automation
- [OBS Studio MCP Server](https://github.com/sbroenne/mcp-server-obs) — Streaming control

## Factual Accuracy

| Claim | Correct Value |
|-------|---------------|
| Screenshot tokens | "thousands of tokens" (not 1500) |
| JPEG quality default | 60 (not 85) |
| Annotated screenshot image | Omitted by default (`includeImage=false`) |
| Toggle state checking | `ui_click` returns state; no `ensure_state` action |
| Copyright | 2024-2026 |

## File References

| Detail | Location |
|--------|----------|
| Complete tool reference | `FEATURES.md` |
| Changelog | `gh-pages/changelog.md` |
| Contributing guide | `CONTRIBUTING.md` |
| Release setup | `.github/RELEASE_SETUP.md` |

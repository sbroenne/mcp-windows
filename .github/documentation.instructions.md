# Documentation Guidelines

This document defines the purpose and content rules for each documentation file to avoid duplication and ensure each serves its intended audience.

## Three Landing Pages

| Document | Audience | Discovery Path |
|----------|----------|----------------|
| `gh-pages/index.md` | General users | Google, windowsmcpserver.dev |
| `vscode-extension/README.md` | VS Code users | VS Code Marketplace |
| `README.md` | Developers | GitHub search, repo browsing |

## Content Rules

### Website (`gh-pages/index.md`)

**Audience**: End users searching for Windows automation solutions

**Tone**: User-friendly, marketing-focused

**Must include**:
- Natural language examples ("Ask your AI to...")
- Feature highlights (card grid)
- Simple installation steps
- Who uses this / use cases
- Links to detailed documentation

**Must NOT include**:
- Code examples (tool call syntax)
- Technical implementation details
- Testing/contributing instructions

### VS Code Extension README (`vscode-extension/README.md`)

**Audience**: VS Code users discovering via Marketplace

**Tone**: Copilot-focused, practical

**Must include**:
- GitHub Copilot-specific language ("Let Copilot control...")
- Natural language examples ("Ask Copilot to...")
- Feature list (Copilot wording)
- Tools table (brief)
- Simple install: "Install this extension"

**Must NOT include**:
- Code examples (tool call syntax)
- Comparison tables (not comparing alternatives)
- Technical configuration details

### GitHub README (`README.md`)

**Audience**: Developers evaluating or contributing

**Tone**: Technical, comprehensive

**Must include**:
- Code examples (tool call syntax)
- Comparison table (why UI Automation)
- Differentiation from other approaches
- Detailed installation options
- Tools table with actions
- MCP configuration examples
- Testing instructions
- Contributing guidelines

**Must NOT include**:
- Natural language examples (those go on user-facing pages)
- Redundant explanations (link to FEATURES.md instead)

## Avoiding Duplication

### Within Pages

- Don't explain the same concept twice
- Comparison tables should appear once per page
- Feature lists should not overlap (e.g., "Smart Screenshots" and "Full Fallback" shouldn't both explain screenshot metadata)

### Across Pages

- Same concepts, different wording for each audience
- Website: "AI agents" → VS Code: "GitHub Copilot" → GitHub: "AI agents/LLMs"
- Website: "Ask your AI to click Save" → VS Code: "Ask Copilot to click Save" → GitHub: `ui_automation(action='click'...)`

## Key Differentiators to Highlight

When explaining why this server is different (GitHub README):

1. **UI Automation first** — find elements by name, not coordinates
2. **Native performance** — .NET, ~50ms response times
3. **Focused** — no shell, file, or process tools (LLM already has those)
4. **Full fallback** — screenshots + mouse/keyboard when needed
5. **Smart screenshots** — include semantic metadata, not just pixels
6. **No telemetry**

## File References

| Feature Details | → | `FEATURES.md` |
| Changelog | → | `gh-pages/changelog.md` |
| Contributing | → | `CONTRIBUTING.md` |

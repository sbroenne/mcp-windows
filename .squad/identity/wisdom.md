---
last_updated: 2026-03-23T00:00:00.000Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

**Pattern:** When a plugin hook discovers a runtime root in inline PowerShell, invoke the real script with `powershell -File ... -PluginRoot <resolved-root>` and let the script validate both the marker file and its own script path. **Context:** Windows PowerShell 5.1 hook flows where CWD and env vars are not trustworthy and silent fallback would break first-run provisioning.

**Pattern:** Never ship a PowerShell hook as `powershell -Command "..."` with unescaped `$variables` inside the quoted payload. Either escape every `$`, switch to a `.ps1` file, or pass a prebuilt command string that contains no live PowerShell variables. **Context:** JSON hook manifests and other command-line launchers where Windows PowerShell will interpolate the double-quoted payload before the scriptblock executes.

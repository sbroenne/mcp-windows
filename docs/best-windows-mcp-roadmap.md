# Roadmap: Windows controls, not just screenshots

## Position

**Give your AI agent Windows controls, not just screenshots.**

Windows MCP gives Copilot, Claude, Cursor, and other compatible agents tools
for Windows apps. It is not a replacement for those agents. The MCP server
and command line use the same automation code.

Lead with three practical benefits:

1. **Find controls, not just pixels:** find actual buttons and fields, then use their returned IDs.
2. **Send useful changes:** avoid repeated full views, read only what is needed,
   and combine steps.
3. **Choose the agent:** use an MCP client or an agent that runs shell commands.

Explain token efficiency as less repeated information for the model to process.
Do not call every alternative screenshot-only: some other Windows MCP servers
also read accessibility information. Do not claim a measured advantage over
GitHub Copilot app computer use without a direct comparison.

Do not compete on raw tool counts, unsupported reliability rankings, or broad
system-administration features that the host already provides. Dedicated browser
and Office tools remain appropriate for their specialized tasks.

## Shipped in the codebase

| Area | Current capability | Boundary |
|------|--------------------|----------|
| Discovery and targeting | Window/subtree snapshots, scoped search, uniqueness checks, observed IDs | Stale IDs fail; they do not retarget similar controls |
| Incremental observations | Automatic differences with safe full fallback | Results depend on the workload; capture still reads the full tree |
| Actions | Click, type, select, bounded waits, batches, optional post-action snapshots | Dispatch is not proof of the application's outcome |
| Data extraction | Element/window reads, article text, table rows/headers, clipboard | Depends on accessible content; explicit window reads can use OCR |
| Windows integration | Window/process management, launch, supported Save As/Open dialogs | English dialog labels and Windows privilege boundaries remain limitations |
| Fallback | Annotated screenshots, mouse, keyboard, continuous drawing strokes | Coordinate-based input still depends on layout and shared focus |
| Reusable sequences | Run supplied batch steps from project-owned files | Not a passive recording of a user's mouse/keyboard session |
| CLI | Shared tool implementation, catalog, persistent CLI service, checked snapshot tokens | MCP and CLI have separate IDs and baselines |
| Quality | Native/WinUI/Electron/Chromium harnesses and a manual model-driven suite | Coverage is not universal application certification |

Installation is a separate concern from implementation. The current release workflow
packages the MCP executable and VS Code extension, not the CLI. The tracked plugin
directory contains skills but no installable manifest or download bootstrap.
See [Installation](https://windowsmcpserver.dev/installation/) for supported routes.

## Priorities

### 1. Evidence and clear documentation

Keep [benchmark results](incremental-snapshot-benchmark.md) tied to measured code,
application versions, comparison baselines, and sample counts. Separate approximate
snapshot tokens from agent billing and capture latency. Preserve unfavorable results.
Refresh measurements when ID formats or response metadata change.

Keep public descriptions and examples aligned with current contracts. Follow the
[documentation guidelines](../.github/documentation.instructions.md), including
the canonical-source model for GitHub Pages.

### 2. Installation consistency

Consider packaging the CLI alongside the MCP server and providing a real installable
plugin if that remains the intended distribution route. Correct the VS Code runtime
acquisition mismatch before promising automatic .NET 10 setup.
These are distribution follow-ups, not completed documentation features.

### 3. Observable, controlled automation

Continue improving explicit outcomes and recovery hints without blindly retrying
side-effecting actions. Evaluate action history, preview support, and optional
confirmation mechanisms against concrete user needs. Tool filters and host approvals
already help limit exposure, but are not desktop isolation or rollback.

### 4. Real application coverage

Prioritize reported failures in native apps, WinUI, Electron, and Chromium.
Keep page controls and browser chrome distinct in coverage claims.
Expand dialog languages and app-specific regressions when a concrete use case
justifies the maintenance cost.

## Deferred choices

- **General UIA event subscriptions:** retain bounded polling where it is reliable.
  Revisit event complexity only when a measured workload justifies it.
- **A separate raw MSAA backend:** UIA already bridges many legacy controls.
  Add another backend only for a demonstrated unsupported application.
- **Generated CLI argument bindings:** shared tool implementations and catalog checks
  already reduce drift. Full code generation remains a separate architecture change.
- **Page-only browser snapshots:** retain the complete-window approach until page
  scoping can reliably preserve the intended content.

## References and sister projects

[Playwright](https://github.com/microsoft/playwright) provides useful patterns for
accessibility snapshots. [FlaUI](https://github.com/FlaUI/FlaUI) and
[pywinauto](https://github.com/pywinauto/pywinauto) are Windows automation references.
These are design references, not evidence that Windows MCP outperforms those projects.

The Excel and PowerPoint MCP servers share the goal of equal MCP/CLI access.
Check their approaches before changing cross-cutting lifecycle or distribution
patterns, and flag relevant fixes for those repositories without assuming identical
implementations.

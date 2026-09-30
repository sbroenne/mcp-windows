# Documentation guidelines

## Writing style

Use simple, jargon-free English throughout user documentation, including feature
and benchmark pages. Write for a new user: what the product does, how to install it,
and a first useful task. Use short sentences and explain necessary technical terms.
Keep command names, option names, and code examples exact.

Describe the current product. Do not fill guides with development history,
rejected experiments, migration notes, or internal test details. Keep release
history in the changelog and detailed test methods in the benchmark reference.

## Core message

> Put your AI to work in Windows.

Lead the homepage with what people can get done: fill forms, read tables, and
save files with the assistant they already use. Support that promise with
direct control, token savings, assistant choice, and screenshots for visual
tasks. "Windows controls, not just screenshots" explains an approach; do not
use it as the main homepage promise.
Explain that the agent can read named buttons, field values, table rows, and
checked states instead of interpreting these only from an image. Describe
changes-only snapshots as a way to avoid sending repeated information.
Keep screenshots available for apps that need them. Compare approaches, not
brands: some other Windows MCP servers also use accessibility information.
Make token savings a leading benefit: read just the controls needed instead of
sending screenshots at every step, then request only changes. Do not weaken this
to vague copy such as "less to process." Keep the claim tied to focused reads,
not a guarantee that every text response is smaller than every screenshot.
Keep comparisons focused on controls and screenshots within Windows MCP.
Do not discuss or compare assistants' own screen-control features. Mention
compatible assistants where useful for MCP setup and installation.

Explain the benefits in plain English: discover actual controls before acting,
send only useful UI changes, and choose MCP or the CLI. Windows MCP is independent
of the agent provider. Copilot, Claude, Cursor, and other compatible clients are
host choices, not the product boundary.

Lead with token savings, precise control targeting, assistant choice, and
screenshot fallback. Put concrete benefits and scoped measured proof before
implementation details. Do not lead marketing pages with reasons not to use
the product or repeat defensive disclaimers after every benefit. Keep meaningful
measurement limits beside the figures and operating limits in the relevant
help or reference section.
Screenshots, mouse, keyboard, and OCR remain useful fallbacks.
Do not dismiss vision-based automation or promise that every application works.

## Audiences and source files

| Surface | Audience and purpose | Source |
|---------|----------------------|--------|
| GitHub README | Developers evaluating the project; short benefits, example, setup links, tools and limitations | `README.md` |
| Website homepage | People discovering Windows automation; plain-language examples and clear setup choices | `gh-pages/docs/index.md` and `gh-pages/overrides/home.html` |
| Search and social previews | Consistent, factual descriptions | `gh-pages/mkdocs.yml`, page front matter, `gh-pages/overrides/main.html` |
| Feature reference | Exact parameters, behavior, errors and examples | `FEATURES.md` |
| CLI guide | Command discovery, syntax, ownership and service lifecycle | `src/Sbroenne.WindowsMcp.Cli/README.md` |
| VS Code listing | Copilot users installing the extension; retain the relevant Copilot focus | `vscode-extension/README.md` and `package.json` |
| Agent skills | Concise operational guidance, not marketing copy | `plugin/skills/*/SKILL.md` |
| Benchmarks | Reproducible measurements with scope and limitations | `docs/incremental-snapshot-benchmark.md` and `docs/screenshot-ui-automation-benchmark.md` |

Keep the README opening short. Detailed click-result and state-management
contracts belong in the feature and CLI references. Use at most four benefit
cards on the website homepage. Preserve useful page URLs and anchors.

Use **MCP or the command line (CLI)** in introductory copy. Introduce `wincli`
as the command-line program's name only when explaining installation or commands.
Do not assume a new reader knows names of executables, protocols, or libraries.

MCP and CLI are both first-class routes for AI agents. Explain that a coding
agent can run `wincli` through its existing terminal tool on the Windows PC;
the user does not have to write scripts. Do not frame MCP as "for AI" and CLI
as "for scripts." Both use the same automation code but keep separate state.
Describe command-help discovery without importing Excel's CLI token-saving
percentage: Windows MCP has no measured CLI-versus-MCP saving yet.

Explain homepage token savings as two everyday choices: read one field rather
than the whole window, and send changes rather than repeat everything. Use the
shared charts, not competing percentage headlines. Keep their different
baselines clear and link to the measurement page for detailed methods.

GitHub Pages uses MkDocs. User guides in `gh-pages/docs/` are written for people
using an assistant. Do not populate them with tool parameter tables, instructions
written for models, developer test notes, or copied release history. Link to
technical references on GitHub instead. Preserve useful URLs when renaming pages.

`gh-pages/hooks.py` generates only the two canonical benchmark includes. The
benchmark page explains the benefit first and keeps each full report in its own
expandable section. Readers must be able to open the results on the site rather
than depend on new documents already being published to GitHub's main branch.
The homepage and benchmark page share chart snippets in `docs/assets/charts/`.
The GitHub README uses SVGs generated from those snippets with
`scripts/generate-benchmark-charts.py`; regenerate them after chart edits.
Never edit or commit `gh-pages/docs/_generated/` or `_site/`.
Update the source and check the rendered page. Include new canonical sources in
the Pages workflow's path filters when needed.

## Evidence and claims

- Link numerical claims to measured workloads, source revision, environment,
  sample count, and comparison baseline.
- Distinguish response bytes, approximate snapshot tokens, capture duration,
  and whole-agent cost. They are not interchangeable.
- Explain both token benefits: short direct reads can use fewer tokens than
  screenshots, and changes-only replies avoid repeating text. Do not apply
  measured full-versus-changes-only savings to image comparisons. A long control
  list is not always smaller than an image; image token counts depend on the model
  and image settings. Keep screenshot support clear.
- Do not add percentages from different optimizations or experiments together.
- Screenshot comparisons must name the model and image detail, count the first
  view and discovery replies, and retain cases where text loses. A focused field
  read is not equivalent to a whole-window view. Do not turn observation-size
  estimates into claims about competing assistants' task success or total bills.
- Publish current benchmark results only. Replace outdated measurements rather
  than keeping historical comparisons on user-facing pages.
- No "works every time," "any app," "100% reliable," unsupported competitor
  rankings, or assumed end-to-end cost/speed savings.
- Describe framework coverage accurately. A harness is not proof that every
  third-party application built on that framework works.
- LLM tests run through a separate manual workflow, not automatically before
  every release. Link the suite instead of copying unmaintained counts or pass rates.
- Check actual release assets and installation files. Do not promise plugin
  bootstrap, automatic runtime setup, or bundled CLI downloads without evidence.
- Use client-specific MCP configuration examples: clients do not all use the
  same JSON root key or installation command.

## Correct interaction model

Discover by name, type, automation ID, or scope, then target the returned opaque
element ID. Confirm syntax against the current tool definitions and CLI help.

```text
ui_find(windowHandle='12345', name='Save', requireUnique=true)
ui_click(windowHandle='12345', elementId='<returned-id>')
```

Selectors are for discovery and appear/disappear waits, not targeted actions.
Stale IDs fail rather than silently choosing a similar control.
IDs belong to their observing MCP process or CLI service; never transfer them
between owners. Macros discover fresh controls instead of storing IDs.

For repeated snapshots, use `mode=auto` from the first observation. A full
fallback is normal when a diff would be unsafe or too large. Separate CLI
commands use the persistent CLI service; pass the previous `snapshotToken`
with `--since` to request a checked diff. The CLI is not stateless.

A successful click dispatch is not proof of saving, submitting, or navigation.
Observe the outcome with snapshots, reads, or bounded waits. Do not recommend
blindly replaying a click after losing its response.

## Safety and privacy

The tools control the real desktop with the process's Windows privileges.
Tool filters limit exposure; they are not an app sandbox or a confirmation system.
Approval behavior depends on the connected host and its configuration.
Local UI processing does not mean the connected AI model receives no data.
Distinguish in-memory state from saved macros and requested screenshot files.

## Related projects

Link Excel MCP Server, PowerPoint MCP Server, and pytest-skill-engineering where
relevant. Describe the shared purpose without assuming identical architectures.
Keep specialized Office automation distinct from Windows desktop UI automation.

## Validation

Build with `python -m mkdocs build --strict --clean` from `gh-pages`.
Review the homepage template, generated references, navigation, links, metadata,
and narrow-screen layout. Do not weaken build checks to accommodate new errors.
Use existing manifest tests when changing extension metadata. Documentation-only
work does not require running model-driven tests.

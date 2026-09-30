# Windows MCP Server — documentation site

The public site at **[windowsmcpserver.dev](https://windowsmcpserver.dev/)** is
built with [MkDocs Material](https://squidfunk.github.io/mkdocs-material/) and
deployed by the `Deploy GitHub Pages` workflow
(`.github/workflows/deploy-gh-pages.yml`).

## User guides and references

Write the website for people using an AI assistant, not for people implementing
the tools. The pages in `docs/` explain current tasks, setup, safety, and help
in plain English. Do not replace them with copies of developer manuals or
instructions written for an AI model.

| Website page | Source and purpose |
|--------------|--------------------|
| Home | `docs/index.md` and `overrides/home.html`: purpose, benefits, shared charts, first task, and setup link |
| Get started | `docs/installation.md`: app-specific steps and a link to CLI setup |
| What you can do | `docs/features.md`: everyday tasks and limitations |
| Help | `docs/troubleshooting.md`, `security.md`, and `privacy.md` |
| Downloads and updates | `docs/changelog.md`: current installation routes, not copied development history |
| How it works | `docs/architecture.md`: a plain-English explanation |
| Compare your options | `docs/comparison.md`: when to use controls or screenshots within Windows MCP |
| Optional instructions | `docs/skills.md`: explain and link skill files, do not paste their contents |
| Help improve Windows MCP | `docs/contributing.md`: ways to help, with a link to developer instructions |
| Measurements | `docs/benchmark.md`: plain explanation with detailed measurements in an expandable section |

`hooks.py` includes measurements from `docs/incremental-snapshot-benchmark.md`,
`docs/screenshot-ui-automation-benchmark.md`, and `docs/real-app-benchmark.md`
at the repository root. It writes the matching reports under
`docs/_generated/` on every build. The measurement page shows reports in separate
expandable sections, so reading the details does not depend on files already
being published to GitHub's main branch.

New real-app results come from `tests/usage_evals/tests/comparison` through
pytest-skill-engineering's native JSON and JUnit reports. Check all 16 cases,
independent saved outputs, unchanged inputs, model settings, usage, and route
restrictions before publishing figures. The old standalone model runners are
retired and their results are not public evidence.
Do not publish raw desktop screenshots, window text, model messages, or local
paths from the private run artifacts.

The main screenshot chart compares live GPT-6 Astra/Luna and GPT-5.6 Sol/Luna
input savings against low-detail images, with checked answers.
Its complete per-read evidence is in `docs/assets/benchmarks/screenshot-readability.json`.
Use `scripts/summarize-screenshot-readability.py` to validate and combine
completed model runs before updating the published figures. It checks that
models received the same inputs and settings and that every trial is retained.
Keep the model comparison separate from the text-update percentages. Generated includes and `_site/`
are ignored by Git; do not edit or commit them.
The homepage and measurement page include the same chart components from
`docs/assets/charts/*.html`. They use HTML and CSS, with readable labels and no
external chart service. The root README uses SVG images generated from those
components. Chart tests check values and scales against both benchmark documents,
verify both pages include the charts, and catch stale README images.

After changing a chart, regenerate and commit its SVG image. Run from the
repository root:

```powershell
python scripts\generate-benchmark-charts.py
```

These SVG images are committed assets, unlike `_generated/` and `_site/`.

Exact tool parameters remain in the root `FEATURES.md`, CLI commands in the
CLI README, and developer instructions in `CONTRIBUTING.md`. Link to those
sources on GitHub when readers need that level of detail.

## Build locally

Run from `gh-pages`:

```powershell
python -m pip install -r requirements.txt
python -m mkdocs serve      # live preview at http://127.0.0.1:8000/
python -m mkdocs build --strict --clean   # production build into _site/
```

Run the include and link tests from the repository root:

```powershell
python -m unittest discover -s gh-pages\tests -v
```

The homepage combines `docs/index.md` and `overrides/home.html`. Update both,
plus the site description in `mkdocs.yml` and relevant metadata in
`overrides/main.html`. Follow [documentation guidelines](../.github/documentation.instructions.md).
Generated includes and `_site/` are build outputs, not editable sources.

The GitHub star-history chart (`docs/assets/images/star-history.svg`) is
generated in CI by `scripts/update_star_history.py`; it is git-ignored.
The user guides do not embed the chart. It is not required for a local build.

The committed `scripts/star-history-bootstrap.json` contains exact cumulative
date/count aggregates produced once from maintainer-authenticated GraphQL
`stargazers.edges.starredAt` data. Raw identities and node IDs are never stored.
The daily workflow reads the public repository `stargazers_count`, appends that
exact aggregate snapshot, and persists the JSON on the dedicated
`star-history-data` branch. This avoids pushing generated data to protected
`main`, using a PAT, or estimating missing history from events or interpolation.

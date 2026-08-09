# Windows MCP Server — documentation site

The public site at **[windowsmcpserver.dev](https://windowsmcpserver.dev/)** is
built with [MkDocs Material](https://squidfunk.github.io/mkdocs-material/) and
deployed by the `Deploy GitHub Pages` workflow
(`.github/workflows/deploy-gh-pages.yml`).

## Single source of truth

Several pages are generated from the authoritative Markdown files elsewhere in
the repo so the site can never drift from the real docs. `hooks.py` runs on
every build (`on_pre_build`) and writes `docs/_generated/*.md`, which the thin
wrapper pages in `docs/` pull in via `pymdownx.snippets` (`--8<--`):

| Source | Generated page |
|--------|----------------|
| `FEATURES.md` | `features.md` |
| `vscode-extension/CHANGELOG.md` | `changelog.md` |
| `CONTRIBUTING.md` | `contributing.md` |

`docs/_generated/` is git-ignored — never edit it by hand; edit the source file.

## Build locally

```bash
pip install -r requirements.txt
mkdocs serve      # live preview at http://127.0.0.1:8000/
mkdocs build --strict --clean   # production build into _site/
```

The GitHub star-history chart (`docs/assets/images/star-history.svg`) is
generated in CI by `scripts/update_star_history.py`; it is git-ignored and does
not need to exist for a local `mkdocs serve`.

The committed `scripts/star-history-bootstrap.json` contains exact cumulative
date/count aggregates produced once from maintainer-authenticated GraphQL
`stargazers.edges.starredAt` data. Raw identities and node IDs are never stored.
The daily workflow reads the public repository `stargazers_count`, appends that
exact aggregate snapshot, and persists the JSON on the dedicated
`star-history-data` branch. This avoids pushing generated data to protected
`main`, using a PAT, or estimating missing history from events or interpolation.

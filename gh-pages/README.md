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
generated in CI by `scripts/Update-StarHistory.ps1`; it is git-ignored and does
not need to exist for a local `mkdocs serve`.

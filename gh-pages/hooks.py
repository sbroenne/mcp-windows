"""MkDocs build hook: generate documentation pages from canonical repo sources.

This preserves the project's single-source-of-truth design: several site pages
are generated from the authoritative Markdown files elsewhere in the repo
(FEATURES.md, the extension CHANGELOG.md, CONTRIBUTING.md) so the website can
never drift from the real docs. It is the MkDocs equivalent of the old Jekyll
``build.sh`` script.

Generated files are written to ``docs/_generated/`` (git-ignored) and pulled
into the thin wrapper pages under ``docs/`` via the ``pymdownx.snippets``
``--8<--`` include syntax. Regeneration happens automatically on every
``mkdocs build`` / ``mkdocs serve`` via the ``on_pre_build`` event.
"""

from __future__ import annotations

import logging
import posixpath
import re
from pathlib import Path

log = logging.getLogger("mkdocs.hooks.generate")

# gh-pages/hooks.py -> gh-pages/ -> repo root
REPO_ROOT = Path(__file__).resolve().parent.parent
GEN_DIR = Path(__file__).resolve().parent / "docs" / "_generated"

GITHUB_BLOB = "https://github.com/sbroenne/mcp-windows/blob/main/"
GITHUB_TREE = "https://github.com/sbroenne/mcp-windows/tree/main/"

# Repo-relative paths that have a dedicated site page: rewrite links to them so
# they resolve on the website instead of 404-ing.
SITE_PAGE_MAP = {
    "FEATURES.md": "/features/",
    "vscode-extension/CHANGELOG.md": "/changelog/",
    "CONTRIBUTING.md": "/contributing/",
    "plugin/skills/windows-automation/SKILL.md": "/skills/",
}

_FRONT_MATTER = re.compile(r"^---\n.*?\n---\n", re.DOTALL)

_MD_LINK = re.compile(r"(?<!!)\[([^\]]+)\]\(([^)\s]+)\)")


def _rewrite_links(text: str, source_rel: str) -> str:
    """Resolve repo-relative links in pulled-in content so they work on the site.

    Links that point at a page we publish are rewritten to that page's URL;
    everything else that resolves inside the repo is rewritten to an absolute
    GitHub URL. External links, anchors and site-absolute links are left alone.
    """
    source_dir = posixpath.dirname(source_rel)

    def repl(match: re.Match) -> str:
        label, url = match.group(1), match.group(2)
        if url.startswith(("http://", "https://", "#", "/", "mailto:", "<")):
            return match.group(0)

        anchor = ""
        target = url
        if "#" in target:
            target, anchor = target.split("#", 1)
            anchor = "#" + anchor
        if target == "":
            return match.group(0)  # pure in-page anchor

        resolved = posixpath.normpath(posixpath.join(source_dir, target))
        if resolved.startswith(".."):
            return match.group(0)  # points outside the repo; leave as-is

        if resolved in SITE_PAGE_MAP:
            return f"[{label}]({SITE_PAGE_MAP[resolved]}{anchor})"

        base = GITHUB_TREE if url.endswith("/") else GITHUB_BLOB
        return f"[{label}]({base}{resolved}{anchor})"

    return _MD_LINK.sub(repl, text)


def _strip_to_first_h2(text: str, *, demote_h1: bool = True) -> str:
    """Drop the leading ``# Title`` + intro block, keeping content from the
    first ``## `` heading onward. Any later ``# `` heading is demoted to ``## ``
    when ``demote_h1`` is set.

    The generated pages get their own H1 from the wrapper page's front matter,
    so the source title and its one-line description are removed to avoid a
    duplicate heading.
    """
    lines = text.splitlines()
    start = next((i for i, ln in enumerate(lines) if ln.startswith("## ")), None)

    if start is None:
        # No H2 in the document: just drop the first H1 line.
        body: list[str] = []
        dropped_h1 = False
        for ln in lines:
            if not dropped_h1 and ln.startswith("# "):
                dropped_h1 = True
                continue
            body.append(ln)
    else:
        body = lines[start:]

    if demote_h1:
        body = [("#" + ln) if ln.startswith("# ") else ln for ln in body]

    return "\n".join(body).strip() + "\n"


def _read(rel: str) -> str:
    path = REPO_ROOT / rel
    if not path.is_file():
        raise FileNotFoundError(f"Source doc not found: {path}")
    return path.read_text(encoding="utf-8")


def _write(name: str, source_rel: str, content: str) -> None:
    GEN_DIR.mkdir(parents=True, exist_ok=True)
    content = _rewrite_links(content, source_rel)
    (GEN_DIR / name).write_text(content, encoding="utf-8")
    log.info("generated _generated/%s", name)


def on_pre_build(config, **kwargs):  # noqa: D401 - MkDocs hook signature
    # FEATURES.md -> features (drop title + intro line, keep from first H2).
    _write(
        "features.md",
        "FEATURES.md",
        _strip_to_first_h2(_read("FEATURES.md")),
    )

    # vscode-extension/CHANGELOG.md -> changelog (drop title + intro, keep
    # from the first version/section H2).
    _write(
        "changelog.md",
        "vscode-extension/CHANGELOG.md",
        _strip_to_first_h2(_read("vscode-extension/CHANGELOG.md")),
    )

    # CONTRIBUTING.md -> contributing (verbatim; keeps its own H1 as the page title).
    _write(
        "contributing.md",
        "CONTRIBUTING.md",
        _read("CONTRIBUTING.md").strip() + "\n",
    )

    # plugin/skills/windows-automation/SKILL.md -> skills (drop YAML front matter;
    # the body already starts at an H2 and the wrapper page supplies the H1).
    _write(
        "skills.md",
        "plugin/skills/windows-automation/SKILL.md",
        _FRONT_MATTER.sub("", _read("plugin/skills/windows-automation/SKILL.md")).strip() + "\n",
    )

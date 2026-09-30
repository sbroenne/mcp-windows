"""Include canonical measurements without copying developer manuals into user guides."""

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
    "docs/incremental-snapshot-benchmark.md": "/benchmark/",
    "docs/screenshot-ui-automation-benchmark.md": "/benchmark/",
    "docs/real-app-benchmark.md": "/benchmark/",
    "gh-pages/docs/assets/benchmarks/real-apps.json": "/assets/benchmarks/real-apps.json",
    "gh-pages/docs/assets/benchmarks/screenshot-readability.json": "/assets/benchmarks/screenshot-readability.json",
}

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


def _strip_first_h1(text: str) -> str:
    """Remove the first H1 while retaining the document introduction."""
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.startswith("# "):
            del lines[index]
            break
    return "\n".join(lines).strip() + "\n"


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
    _write(
        "real-app-benchmark.md",
        "docs/real-app-benchmark.md",
        _strip_first_h1(_read("docs/real-app-benchmark.md")),
    )
    _write(
        "benchmark.md",
        "docs/incremental-snapshot-benchmark.md",
        _strip_first_h1(_read("docs/incremental-snapshot-benchmark.md")),
    )
    _write(
        "screenshot-benchmark.md",
        "docs/screenshot-ui-automation-benchmark.md",
        _strip_first_h1(_read("docs/screenshot-ui-automation-benchmark.md")),
    )

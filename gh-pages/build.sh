#!/bin/bash
# Jekyll build script for Windows MCP Server documentation
# This script copies shared content files before building Jekyll
# Used by both local development and GitHub Actions

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

echo "📁 Copying shared content files..."

# Create _includes directory if it doesn't exist
mkdir -p "$SCRIPT_DIR/_includes"

# Copy CHANGELOG.md from vscode-extension if it exists
# Strip top H1 block (title + paragraph) and convert remaining H1 to H2
if [ -f "$ROOT_DIR/vscode-extension/CHANGELOG.md" ]; then
    awk '
        BEGIN { inheader=0; headerdone=0 }
        {
            if (headerdone==0 && /^# /) { inheader=1; next }                 # drop H1 title
            if (inheader==1 && /^All notable/) { next }                      # drop description line
            if (inheader==1 && /^$/) { inheader=0; headerdone=1; next }      # blank line ends header
            if (/^# /) { sub(/^# /, "## "); print; next }                   # convert any remaining H1 → H2
            print
        }
    ' "$ROOT_DIR/vscode-extension/CHANGELOG.md" > "$SCRIPT_DIR/_includes/changelog.md"
    echo "   ✓ Copied CHANGELOG.md (stripped top H1 block, H1→H2)"
fi

# Copy CONTRIBUTING.md from root
if [ -f "$ROOT_DIR/CONTRIBUTING.md" ]; then
    cp "$ROOT_DIR/CONTRIBUTING.md" "$SCRIPT_DIR/_includes/contributing.md"
    echo "   ✓ Copied CONTRIBUTING.md"
fi

# Determine build mode
if [ "$1" == "serve" ]; then
    echo ""
    echo "🚀 Starting Jekyll server..."
    cd "$SCRIPT_DIR"
    bundle exec jekyll serve --host 127.0.0.1 --port 4000
elif [ "$1" == "production" ] || [ "$JEKYLL_ENV" == "production" ]; then
    echo ""
    echo "🏗️  Building for production..."
    cd "$SCRIPT_DIR"
    JEKYLL_ENV=production bundle exec jekyll build
else
    echo ""
    echo "🏗️  Building for development..."
    cd "$SCRIPT_DIR"
    bundle exec jekyll build
fi

echo ""
echo "✅ Build complete!"

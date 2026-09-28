---
title: Downloads and updates
description: Where to get Windows MCP and how to update your installation.
---

# Downloads and updates

## VS Code extension

Get updates through VS Code's Extensions view. The
[Marketplace page](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp)
lists the available extension version.

## MCP server download

[Open the latest release on GitHub](https://github.com/sbroenne/mcp-windows/releases/latest)
for the Windows x64 and ARM64 downloads and their release notes.

Stop active automation tasks before updating. Close the AI app that runs the
server, extract the new download into its installation folder, and then reopen
the AI app. Check that its settings still point to `Sbroenne.WindowsMcp.exe`.

[Installation instructions](installation.md)

## Command line (CLI)

Follow the [CLI update instructions](https://github.com/sbroenne/mcp-windows/blob/main/src/Sbroenne.WindowsMcp.Cli/README.md#service-lifecycle-and-deployment).
Stop its background service before replacing the program files.

---
name: "mcp-client-distribution"
description: "Guide for making this repo's Windows MCP server discoverable and installable from GitHub Copilot CLI, Claude Code, Claude Desktop, and related MCP client ecosystems. Use whenever the user mentions Copilot CLI support, Claude Code support, plugin support, marketplace listing, MCP Registry publishing, awesome-copilot, client install docs, or release/distribution work for this server."
domain: "distribution"
confidence: "medium"
source: "repo-specific"
---

## Context

This repository publishes a **Windows MCP server**. For client integration work, the primary concepts are:

- **MCP server** — the executable this repo ships
- **Client config** — how Copilot CLI / Claude clients launch the server
- **Registry / listing metadata** — how users discover it

Do **not** assume a separate binary, SDK mode, or plugin bundle is required just because the user says "plugin". For this repo, "plugin support" usually means one of:

- clearer client setup docs
- release assets that are easy to configure
- registry metadata and publication
- community/catalog listing entries

## Repo-Specific Facts

### Server Shape

- The server is a .NET executable: `src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj`
- It already builds as an `Exe`
- Standalone single-file Windows binaries are produced by `build-standalone.ps1`
- Expected executable name: `Sbroenne.WindowsMcp.exe`

### Existing Client Surfaces

- `README.md` already has a section for `Copilot CLI / Claude Desktop / Other MCP Clients`
- `MCP_CLIENT_SETUP.md` already contains manual configuration examples
- `.copilot/mcp-config.json` already shows repo-level Copilot MCP config format

### Current Packaging Assumption

The main install path is: **download the standalone executable from Releases and point the client config at it**.

That means most future work in this area is likely **docs + metadata + release hygiene**, not protocol or tool code changes.

## When To Use This Skill

Use this skill whenever the user asks for any of the following:

- "Add Copilot CLI support"
- "Add Claude Code support"
- "Make this installable as a plugin"
- "List this in awesome-copilot"
- "Publish to the MCP Registry"
- "Improve marketplace/discovery"
- "What do we need for Copilot / Claude distribution?"

Also use it when an agent starts proposing code changes for client support without first checking whether docs, metadata, or release assets are the real gap.

## Working Model

### 1. Normalize the Terminology First

Start by translating fuzzy terms into concrete targets:

- "plugin" -> usually MCP server registration or client configuration
- "Copilot CLI support" -> Copilot MCP config + discovery docs + registry metadata
- "Claude Code support" -> Claude MCP config + discovery docs + registry metadata
- "marketplace" -> determine whether the target is official, community-run, or a registry/catalog

If the user's target is ambiguous, clarify **which surface** they care about:

- manual config from Releases
- official MCP Registry discoverability
- awesome-copilot listing
- another client-specific gallery/catalog

### 2. Prefer Metadata and Docs Before Code

For this repo, the default order is:

1. verify the server already runs as a stdio MCP executable
2. verify release assets exist and are named clearly
3. add or fix install docs
4. add machine-readable metadata for registries/catalogs
5. only then consider code changes

Do **not** jump to implementation work inside the server unless a client has a real compatibility requirement.

### 3. Check the Required Artifacts

When preparing this repo for Copilot / Claude client distribution, inspect:

- `README.md`
- `MCP_CLIENT_SETUP.md`
- `.copilot/mcp-config.json`
- `build-standalone.ps1`
- release workflow / published assets
- any registry metadata file such as `server.json` if introduced

## Recommended Deliverables For This Repo

### Minimum Viable Path

If the goal is "users can install this from the repo", focus on:

- clear release assets for x64 / arm64
- copy-paste client config snippets
- one canonical setup guide
- verification steps such as `--version`

### Registry / Discovery Path

If the goal is discoverability in Copilot / Claude ecosystems, focus on:

- machine-readable MCP metadata
- official registry publication steps
- listing/readme wording aligned with MCP terminology
- optional catalog submission such as `awesome-copilot`

### Good Future Additions

- a repo-root metadata file for MCP Registry publication if not present
- a dedicated README section for "Discovery and Registry"
- release notes that call out supported client ecosystems explicitly

## Copilot / Claude Guidance

### GitHub Copilot CLI

Treat Copilot support as:

- valid MCP config examples
- accurate config file location guidance
- registry/listing metadata if the ecosystem supports discovery from a central registry

Do not assume "plugin package" means a separate artifact beyond the MCP server unless official docs say so.

### Claude Code / Claude Desktop

Treat Claude support as:

- valid MCP config examples
- accurate client-specific configuration guidance
- MCP Registry compatibility where supported

Do not conflate:

- Claude Code
- Claude Desktop
- unofficial marketplace concepts

Check the exact product surface before promising a listing path.

## Marketplace and Catalog Discipline

When a user mentions marketplace targets:

### MCP Registry

This is the first-class discovery target for MCP servers. If registry publication is available, prefer it over bespoke "plugin" packaging.

### awesome-copilot

Treat this as a **catalog/listing target**, not the primary integration mechanism. The integration mechanism is still the MCP server + client config/registry metadata.

### "Claude Plugin Marketplace"

Do not assume this exists as a first-class official submission target for this repo's server. Verify it with current docs before proposing work.

## Anti-Patterns

- **Assuming "plugin" means new server code** — distribution work is usually docs/metadata first.
- **Treating this repo as a client** — it is the server being consumed by clients.
- **Merging Copilot CLI, Claude Code, and Claude Desktop into one vague target** — confirm the exact product surface.
- **Promising unofficial marketplaces as official** — verify before recommending.
- **Adding packaging complexity too early** — prefer release asset + config + registry metadata unless a client explicitly requires more.

## Output Shape

When using this skill, structure your answer as:

1. **Verdict** — already compatible vs missing distribution artifacts
2. **MVP path** — smallest set of changes
3. **Optional discovery path** — registry/catalog work
4. **Assumptions to avoid** — especially around "plugin" wording

## Examples

### Example: User asks for Copilot CLI plugin support

Interpretation:

- likely means install docs + Copilot MCP config + registry/discovery metadata
- not a new plugin runtime

### Example: User wants inclusion in awesome-copilot

Interpretation:

- catalog/listing task
- likely requires a clean repo description, install steps, and stable release assets
- should not be confused with the actual integration mechanism

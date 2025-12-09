# Data Model: Release Management

**Feature**: 009-release-management  
**Date**: 2025-12-09  
**Phase**: 1 - Design

## Entities

### 1. Version Tag

**Description**: Git tag that triggers a release workflow

**Attributes**:
| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Full tag name (e.g., `mcp-v1.2.3`, `vscode-v1.2.3`) |
| `prefix` | string | Tag prefix identifying release type (`mcp-v` or `vscode-v`) |
| `version` | string | Semantic version extracted from tag (e.g., `1.2.3`) |
| `sha` | string | Git commit SHA the tag points to |

**Validation Rules**:
- Tag name MUST match pattern `^(mcp|vscode)-v\d+\.\d+\.\d+(-[\w.]+)?$`
- Version MUST be valid semver (MAJOR.MINOR.PATCH with optional prerelease)

**State Transitions**: N/A (immutable once created)

---

### 2. Workflow Run

**Description**: A single execution of a release workflow

**Attributes**:
| Field | Type | Description |
|-------|------|-------------|
| `run_id` | string | GitHub Actions run identifier |
| `workflow_name` | string | Name of the workflow (`Release MCP Server` or `Release VS Code Extension`) |
| `trigger_tag` | VersionTag | The tag that triggered the run |
| `status` | enum | `queued`, `in_progress`, `success`, `failure`, `cancelled` |
| `started_at` | datetime | When the run started |
| `completed_at` | datetime | When the run completed |

**Validation Rules**:
- `completed_at` MUST be after `started_at`
- `status` can only transition forward (queued → in_progress → terminal state)

---

### 3. Release Artifact

**Description**: Build output attached to a GitHub release

**Attributes**:
| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Artifact filename (e.g., `windows-mcp-server-1.2.3.zip`) |
| `type` | enum | `zip` (MCP server) or `vsix` (VS Code extension) |
| `size_bytes` | integer | File size in bytes |
| `sha256` | string | SHA-256 checksum of the artifact |
| `download_url` | string | GitHub release asset download URL |

**Validation Rules**:
- `name` MUST include version number
- `size_bytes` MUST be > 0

---

### 4. GitHub Release

**Description**: Published release on GitHub with artifacts and notes

**Attributes**:
| Field | Type | Description |
|-------|------|-------------|
| `tag_name` | string | Associated git tag |
| `title` | string | Release title displayed on GitHub |
| `body` | string | Release notes (Markdown) |
| `artifacts` | ReleaseArtifact[] | Attached downloadable files |
| `created_at` | datetime | When the release was published |
| `draft` | boolean | Whether release is a draft (always `false` for this workflow) |
| `prerelease` | boolean | Whether this is a prerelease (determined by version suffix) |

**Validation Rules**:
- `tag_name` MUST exist and point to a valid commit
- `artifacts` MUST contain at least one file
- `prerelease` is `true` if version contains `-` suffix (e.g., `1.0.0-beta.1`)

---

### 5. Marketplace Publication

**Description**: VS Code extension published to the Marketplace

**Attributes**:
| Field | Type | Description |
|-------|------|-------------|
| `extension_id` | string | Marketplace identifier (`sbroenne.windows-mcp`) |
| `version` | string | Published version |
| `status` | enum | `success`, `failed`, `skipped` |
| `error_message` | string? | Error details if status is `failed` |
| `published_at` | datetime? | When successfully published |

**Validation Rules**:
- `error_message` is only set when `status` is `failed`
- `published_at` is only set when `status` is `success`

---

## Relationships

```
VersionTag 1──triggers──1 WorkflowRun
WorkflowRun 1──produces──* ReleaseArtifact
WorkflowRun 1──creates──1 GitHubRelease
WorkflowRun 0..1──publishes──1 MarketplacePublication (extension only)
GitHubRelease 1──contains──* ReleaseArtifact
```

## Environment Variables

| Variable | Scope | Description |
|----------|-------|-------------|
| `VERSION` | Workflow | Extracted version from tag (set via `GITHUB_ENV`) |
| `VSIX_PATH` | Extension workflow | Path to built VSIX file |
| `PACKAGE_VERSION` | Extension workflow | npm package version after update |

## Secrets

| Secret | Required By | Description |
|--------|-------------|-------------|
| `GITHUB_TOKEN` | Both workflows | Auto-provided, needs `contents: write` |
| `VSCE_TOKEN` | Extension workflow | Azure DevOps PAT for Marketplace |

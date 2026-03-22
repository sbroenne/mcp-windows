---
name: "chromium-local-smoke-tests"
description: "Pattern for deterministic first-slice Chromium browser coverage using a local Edge page instead of public websites"
domain: "testing"
confidence: "high"
source: "earned"
---

## Context
Use this when the team wants to start real-browser coverage but needs the first slice to stay small, deterministic, and safe for PR validation.

## Pattern
- Prefer **Edge + local static HTML** over public sites for the first browser slice.
- Keep the scope to **page content discovery** first: landmarks, ARIA-labeled inputs, and buttons.
- Gate the slice with an **opt-in env var** so it is shippable immediately without destabilizing default desktop runs.
- Launch the browser with an isolated `--user-data-dir` and `--force-renderer-accessibility`.

## Good first assertions
- Landmark or navigation container is discoverable.
- ARIA-labeled search box resolves as `Edit`.
- ARIA-labeled primary action resolves as `Button`.

## Anti-patterns
- Do not start with public practice sites for gating coverage.
- Do not mix address-bar/tab-strip/browser-chrome assertions into the first slice.
- Do not rely on existing user browser profiles or ambient browser state.

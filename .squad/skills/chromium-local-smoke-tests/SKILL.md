---
name: "chromium-local-smoke-tests"
description: "Pattern for always-on Chromium browser smoke coverage using deterministic local Edge/Chrome pages plus a stable public-web slice"
domain: "testing"
confidence: "high"
source: "earned"
---

## Context
Use this when the team wants a small, always-on Chromium browser slice that proves real page interaction honestly without creating browser-specific MCP tools.

## Pattern
- Prefer **Edge and Chrome + local static HTML** for the deterministic slice, then pair them with one **stable public-web page** for required internet coverage.
- Keep the scope to **page content discovery** first: landmarks, ARIA-labeled inputs, and buttons.
- Launch Chromium app windows with startup hygiene, `--force-renderer-accessibility`, and an **isolated `--user-data-dir`** for deterministic smoke reliability.
- Keep the public tier to a **stable browser-testing page** (for example `https://demo.playwright.dev/todomvc/`) and assert only long-lived page content such as labeled inputs or links.
- Keep browser chrome, logins, and ambient profile state out of the default smoke slice.
- For manual checks against authenticated or SSO-only sites, **reuse an already-open signed-in Edge/Chrome window first** instead of relaunching the URL. Treat a Chromium launcher stub exiting immediately as normal until you confirm whether the existing browser session already opened the page.

## Good first assertions (local tier)
- Landmark or navigation container is discoverable.
- ARIA-labeled search box resolves as `Edit`.
- ARIA-labeled primary action resolves as `Button`.

## Good second-slice assertions (local interaction tier)
- Add one **editable field** whose value can be typed and read back to prove `ui_type` + `ui_read`.
- Add one **always-visible text node** to prove baseline `ui_read` on Chromium page content.
- Add one **page-owned post-click assertion** (revealed text, changed text, toggled state) to prove `ui_click` affected the DOM, not just that the automation layer returned success.
- Keep the post-click assertion inside the page content itself; avoid browser chrome, URL assertions, tabs, or address bar checks.

## Harness implementation note
- Use app-window launch (`--app=`) plus accessibility/startup hygiene and an isolated profile, then wait for **all page-owned readiness selectors** before running assertions. Only if readiness does not appear should the test harness use a narrow, test-only dismissal path for known Edge first-run/sync dialogs; if a sync popup appears, look specifically for a `"Got it"` button and dismiss that exact button before proceeding.
- Close the launched Edge window first, then kill the dedicated temp-profile browser process tree only if it fails to exit promptly. Delete the temp profile after teardown.

## Good public-site assertions (smoke tier)
- Canonical placeholder or label text as accessible name (e.g., TodoMVC `What needs to be done?` → `Edit`).
- Longstanding links or buttons by visible text + stable control type.
- Prefer public demos maintained by browser/tooling vendors over individual-maintained practice sites.
- Use 15s+ timeouts for public pages (vs 5s local) to absorb network latency.

## Anti-patterns
- Do not depend on public practice sites whose copy or uptime is outside your control.
- Do not mix address-bar/tab-strip/browser-chrome assertions into the first slice.
- Do not rely on ambient browser state for default coverage; use an isolated browser profile.
- Do not accept `clickResult.Success == true` as sufficient proof of Chromium interaction; require a page-content effect.

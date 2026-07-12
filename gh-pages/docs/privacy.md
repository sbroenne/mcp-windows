---
title: Privacy
description: Windows MCP Server runs locally, does not collect telemetry and does not send your data anywhere. What stays on your machine and the few times it touches the network.
keywords: "Windows MCP privacy, no telemetry, local automation, data collection, offline MCP server"
---

# Privacy

Windows MCP Server is built to run **entirely on your machine**. It is a local
process that your MCP client starts and talks to over stdio — there is no hosted
backend and no account to sign up for.

## No telemetry

The server does not collect analytics or telemetry and does not phone home. It
contains no analytics SDK and sends no usage data to the author or any third
party.

## Your data stays local

Everything the server reads or acts on — window contents, UI Automation trees,
text read via OCR, and screenshots — is processed locally and returned only to
the MCP client that made the request. The server does not persist this data or
transmit it anywhere itself.

!!! note "Your AI client is separate"
    The AI assistant you connect to the server (GitHub Copilot, Claude Desktop,
    Cursor, and similar) has its own privacy policy. When you ask it to automate
    something, the tool results — which can include on-screen text and
    screenshots — are sent to that assistant's model so it can decide what to do
    next. Review your AI client's privacy terms to understand how it handles that
    data.

## When it touches the network

The core server does not require network access. There are only a couple of
optional, clearly-scoped cases where components reach the internet:

- **Downloading a release** — the plugin downloads the standalone server build
  from GitHub Releases the first time you use it.
- **Development and testing** — the project's LLM test suite calls Azure OpenAI,
  but that runs only when contributors run the tests. It is not part of using the
  server.

## Open source

The full source is available at
[github.com/sbroenne/mcp-windows](https://github.com/sbroenne/mcp-windows) under
the MIT license, so you can verify exactly what the server does.

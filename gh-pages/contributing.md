---
layout: default
title: "Contributing"
description: "How to contribute to Windows MCP Server development. Guidelines for pull requests, code style, and community participation."
permalink: /contributing/
---

<div class="hero">
  <div class="container">
    <div class="hero-content">
      <h1 class="hero-title">Contributing</h1>
      <p class="hero-subtitle">Guidelines for contributing to Windows MCP Server</p>
    </div>
  </div>
</div>

{% capture contributing_content %}{% include contributing.md %}{% endcapture %}

{{ contributing_content | markdownify }}

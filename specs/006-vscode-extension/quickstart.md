# Quickstart: VS Code Extension for Windows MCP Server

**Date**: 2025-12-08 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Installation

### From VSIX (Local Build)

1. Build the extension:
   ```powershell
   cd vscode-extension
   npm install
   npm run compile
   npm run build:mcp-server
   npm run package
   ```

2. Install in VS Code:
   - Open VS Code
   - Press `Ctrl+Shift+P` → "Extensions: Install from VSIX..."
   - Select `sbroenne-windowsmcp-1.0.0.vsix`

### From Marketplace (Future)

```
ext install sbroenne.sbroenne-windowsmcp
```

## Usage

After installation, the Windows MCP Server is automatically available to AI assistants like GitHub Copilot.

### Verify Installation

1. Open the Output panel (`Ctrl+Shift+U`)
2. Select "Windows MCP Server" from the dropdown
3. You should see: "Windows MCP Server extension activated"

### Available Tools

The MCP server provides these tools to AI assistants:

| Tool | Description |
|------|-------------|
| `mouse_control` | Move, click, double-click, drag, scroll |
| `keyboard_control` | Type text, press keys, key combinations |
| `window_control` | Focus, minimize, maximize, resize windows |
| `screenshot` | Capture screen, window, or region |

### Example Prompts for Copilot

```
"Click the Start button"
"Type 'Hello World' in the active window"
"Take a screenshot of the current window"
"Move the mouse to 500, 300"
```

## Development

### Build Commands

| Command | Purpose |
|---------|---------|
| `npm install` | Install dependencies |
| `npm run compile` | Compile TypeScript |
| `npm run build:mcp-server` | Build and bundle MCP server |
| `npm run package` | Create VSIX package |

### Project Structure

```
vscode-extension/
├── src/extension.ts    # Extension entry point
├── bin/                # Bundled MCP server (generated)
├── out/                # Compiled JS (generated)
├── package.json        # Extension manifest
└── tsconfig.json       # TypeScript config
```

## Troubleshooting

### Extension Not Activating

- Ensure VS Code version is 1.106.0 or later
- Ensure you're on Windows (extension is Windows-only)

### .NET Runtime Error

The extension depends on `ms-dotnettools.vscode-dotnet-runtime`. If you see runtime errors:
1. Install the .NET Install Tool extension manually
2. Reload VS Code

### MCP Server Not Available

1. Check the Output panel for errors
2. Ensure the server binary exists in `bin/` folder
3. Try reinstalling the extension

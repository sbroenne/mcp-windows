---
title: Get started
description: Set up Windows MCP with your AI app, then try a small task on your Windows PC.
---

# Get started

You can use Windows MCP through **MCP or the command line (CLI)**.
Both routes work with AI agents. MCP connects the Windows tools to your AI app.
An agent that can run commands on your Windows PC can use the CLI through its
existing terminal tool. You do not have to write a script.

Start with your AI app below, or use the
[CLI setup for coding agents](#command-line-cli).

## What you need

- A Windows PC with a signed-in, unlocked desktop.
- An AI app with MCP support, or an agent that can run commands on your Windows PC.
- Permission to install software and change that app's settings.

Windows MCP is free. Your AI app may need its own subscription.
Read [how to use it safely](security.md) before allowing it to act.

<span id="vs-code-github-copilot"></span>
## GitHub Copilot in VS Code

1. Install [Windows MCP Server from the VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp).
2. Install the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0). This is Microsoft software needed to run the server included with the extension.
3. Open Copilot Chat in VS Code and choose **Agent** mode.
4. Enable the Windows MCP tools in the chat's tool list.
5. Try the [first task below](#try-your-first-task).

You do not need to add a second copy of the server through a settings file.

<span id="standalone-any-mcp-client"></span>
## Claude Desktop, Cursor, and other AI apps

### Download the server

1. Open [the latest release](https://github.com/sbroenne/mcp-windows/releases/latest).
2. Download the Windows ZIP file for your PC: **x64** for most PCs, or **ARM64** for an ARM-based PC. Windows **Settings > System > About > System type** shows which you have.
3. Extract the ZIP into a folder you will keep, for example `C:\Tools\windows-mcp`.
4. Find `Sbroenne.WindowsMcp.exe` in that folder. You will need its full path.

Keep all the extracted files together. This download includes the Microsoft
software it needs to run; you do not need a separate .NET installation.

### Connect your AI app

Choose the matching instructions. The example path below assumes you extracted
the files into `C:\Tools\windows-mcp`. Replace it if you chose a different folder.

=== "Claude Desktop"

    Open **Settings > Developer > Edit Config**. This opens
    `%APPDATA%\Claude\claude_desktop_config.json`.

    Add the `windows` entry under `mcpServers`. If the file already contains
    other servers, keep them.

    ```json
    {
      "mcpServers": {
        "windows": {
          "command": "C:\\Tools\\windows-mcp\\Sbroenne.WindowsMcp.exe"
        }
      }
    }
    ```

    Save the file and restart Claude Desktop. Check that Windows MCP appears
    in its tool settings.

=== "Cursor"

    Open or create `%USERPROFILE%\.cursor\mcp.json` to make the tools available
    across your projects. Use `.cursor\mcp.json` inside a project if you want
    the settings to apply only there.

    Add the `windows` entry under `mcpServers`. Keep any other server entries.

    ```json
    {
      "mcpServers": {
        "windows": {
          "command": "C:\\Tools\\windows-mcp\\Sbroenne.WindowsMcp.exe"
        }
      }
    }
    ```

    Save the file, reload Cursor, and check its MCP settings for Windows MCP.

=== "Other AI apps"

    Open your app's MCP settings and add a server that runs on your computer.
    Give it the full path to `Sbroenne.WindowsMcp.exe`.

    The settings format varies by app. Use that app's setup instructions
    rather than copying another app's settings file.

    In GitHub Copilot CLI, start with `/mcp add`.
    For Claude Code, you can run:

    ```powershell
    claude mcp add windows -- "C:\Tools\windows-mcp\Sbroenne.WindowsMcp.exe"
    ```

## Try your first task

Open Notepad with a new, empty document. Ask your assistant:

> Type "Hello from Windows MCP" in Notepad and save it as a new file named
> windows-mcp-demo.txt on my Desktop.

Read any permission prompt before approving it. Leave the mouse and keyboard
alone while the task runs. When it finishes, check the file on your Desktop.

[More tasks to try](features.md) | [Help if something does not work](troubleshooting.md)

<span id="cli-for-agents-with-shell-access"></span>
## Command line (CLI)

The command-line program is named `wincli`. It provides the same Windows tools
for coding agents, other assistants with command access, and scripts. Once it
is installed, your agent can discover and run its commands using its existing
terminal tool. You do not need to register a Windows MCP server for this route.

It is installed by building it from the source code. Follow the
[CLI installation guide](https://github.com/sbroenne/mcp-windows/blob/main/src/Sbroenne.WindowsMcp.Cli/README.md).
The server ZIP download above is for MCP, not the CLI.

## Optional instructions for your assistant

Some AI apps can load extra instruction files called **skills**. These can
help an assistant use the Windows tools, but they do not install the server.
They are not required for the first task.

[About the optional instruction files](skills.md)

## Advanced settings

If you need to choose which tools are available, see the
[command and settings reference](https://github.com/sbroenne/mcp-windows/blob/main/FEATURES.md#configuration).
Hiding tools does not restrict the assistant to a particular app or folder.

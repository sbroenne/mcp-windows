import * as vscode from 'vscode';
import * as path from 'path';

/**
 * Windows MCP VS Code Extension
 *
 * This extension provides MCP server definitions for the Windows MCP server,
 * enabling AI assistants like GitHub Copilot to control mouse, keyboard,
 * windows, and capture screenshots on Windows.
 */

export async function activate(context: vscode.ExtensionContext) {
    console.log('WindowsMcp extension is now active');

    // Register MCP server definition provider
    context.subscriptions.push(
        vscode.lm.registerMcpServerDefinitionProvider('windows-mcp', {
            provideMcpServerDefinitions: async () => {
                // Return the MCP server definition for WindowsMcp
                const extensionPath = context.extensionPath;
                const mcpServerPath = path.join(extensionPath, 'bin', 'Sbroenne.WindowsMcp.exe');

                return [
                    new vscode.McpStdioServerDefinition(
                        'Windows MCP Server',
                        mcpServerPath,
                        [],
                        {
                            // Optional environment variables can be added here if needed
                        }
                    )
                ];
            }
        })
    );

    // Show welcome message on first activation
    const hasShownWelcome = context.globalState.get<boolean>('windowsmcp.hasShownWelcome', false);
    if (!hasShownWelcome) {
        showWelcomeMessage();
        context.globalState.update('windowsmcp.hasShownWelcome', true);
    }
}

function showWelcomeMessage() {
    const message = 'Windows MCP extension activated! The Windows MCP server is now available for AI assistants.';
    const learnMore = 'Learn More';

    vscode.window.showInformationMessage(message, learnMore).then(selection => {
        if (selection === learnMore) {
            vscode.env.openExternal(vscode.Uri.parse('https://github.com/sbroenne/mcp-windows'));
        }
    });
}

export function deactivate() {
    console.log('WindowsMcp extension is now deactivated');
}

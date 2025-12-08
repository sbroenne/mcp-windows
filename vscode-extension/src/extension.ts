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

    // Ensure .NET runtime is available (still needed for the bundled executable)
    try {
        await ensureDotNetRuntime();
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        vscode.window.showErrorMessage(
            `WindowsMcp: Failed to setup .NET environment: ${errorMessage}. ` +
            `The extension may not work correctly.`
        );
    }

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

async function ensureDotNetRuntime(): Promise<void> {
    try {
        // Request .NET runtime acquisition via the .NET Install Tool extension
        const dotnetExtension = vscode.extensions.getExtension('ms-dotnettools.vscode-dotnet-runtime');

        if (!dotnetExtension) {
            throw new Error('.NET Install Tool extension not found. Please install ms-dotnettools.vscode-dotnet-runtime');
        }

        if (!dotnetExtension.isActive) {
            await dotnetExtension.activate();
        }

        // Request .NET 8 runtime using the command-based API
        // The extension uses commands, not direct exports
        const requestingExtensionId = 'sbroenne.windows-mcp';

        await vscode.commands.executeCommand('dotnet.showAcquisitionLog');
        const result = await vscode.commands.executeCommand<{ dotnetPath: string }>('dotnet.acquire', {
            version: '8.0',
            requestingExtensionId
        });

        if (result?.dotnetPath) {
            console.log(`WindowsMcp: .NET runtime available at ${result.dotnetPath}`);
        }

        console.log('WindowsMcp: .NET runtime setup completed (MCP server is bundled with extension)');
    } catch (error) {
        console.error('WindowsMcp: Error during .NET runtime setup:', error);
        throw error;
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

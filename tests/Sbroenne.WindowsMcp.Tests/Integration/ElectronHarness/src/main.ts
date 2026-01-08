import { app, BrowserWindow, ipcMain, dialog, globalShortcut } from 'electron';
import * as path from 'path';
import * as fs from 'fs';

let mainWindow: BrowserWindow | null = null;
let lastSavePath: string | null = null;

function createWindow(): void {
    mainWindow = new BrowserWindow({
        width: 900,
        height: 700,
        title: 'MCP Electron Test Harness',
        webPreferences: {
            preload: path.join(__dirname, 'preload.js'),
            contextIsolation: true,
            nodeIntegration: false,
        },
    });

    mainWindow.loadFile(path.join(__dirname, '..', 'index.html'));

    // Ensure accessibility is enabled on the web contents after load
    mainWindow.webContents.on('did-finish-load', () => {
        // Execute JavaScript to trigger accessibility tree building
        mainWindow?.webContents.executeJavaScript(`
            // Force accessibility tree to be built
            document.body.setAttribute('role', 'application');

            // Ensure all interactive elements have accessibility properties
            const buttons = document.querySelectorAll('button');
            buttons.forEach((btn, i) => {
                if (!btn.getAttribute('role')) btn.setAttribute('role', 'button');
                if (!btn.getAttribute('aria-label') && !btn.textContent?.trim()) {
                    btn.setAttribute('aria-label', 'Button ' + i);
                }
            });

            const inputs = document.querySelectorAll('input, textarea');
            inputs.forEach((input, i) => {
                if (!input.id) input.id = 'input-' + i;
            });

            console.log('[A11y] Accessibility attributes applied');
        `).catch(() => {});
    });

    mainWindow.on('closed', () => {
        mainWindow = null;
    });
}

// Shared save dialog function - can be called from IPC or keyboard shortcut
async function showSaveDialog(): Promise<{ success: boolean; filePath?: string; cancelled?: boolean; error?: string }> {
    if (!mainWindow) {
        return { success: false, error: 'No window available' };
    }

    const result = await dialog.showSaveDialog(mainWindow, {
        title: 'Save As',
        defaultPath: 'document.txt',
        filters: [
            { name: 'Text Files', extensions: ['txt'] },
            { name: 'All Files', extensions: ['*'] },
        ],
    });

    if (result.canceled || !result.filePath) {
        mainWindow.webContents.send('status-update', 'Save cancelled');
        return { success: false, cancelled: true };
    }

    // Write test content to the file
    const content = `Test file created at ${new Date().toISOString()}\nEditor content: (from Electron harness)`;
    try {
        fs.writeFileSync(result.filePath, content, 'utf-8');
        lastSavePath = result.filePath;
        mainWindow.webContents.send('status-update', `Saved to: ${path.basename(result.filePath)}`);
        return { success: true, filePath: result.filePath };
    } catch (err) {
        const errorMessage = err instanceof Error ? err.message : String(err);
        mainWindow.webContents.send('status-update', `Save failed: ${errorMessage}`);
        return { success: false, error: errorMessage };
    }
}

function setupIpcHandlers(): void {
    // IPC handlers for test verification
    ipcMain.handle('get-state', () => {
        // Return current application state for test verification
        return {
            isReady: true,
            windowTitle: mainWindow?.getTitle() ?? '',
            lastSavePath: lastSavePath,
        };
    });

    ipcMain.handle('reset-state', () => {
        // Reset application state
        lastSavePath = null;
        mainWindow?.webContents.send('reset');
        return { success: true };
    });

    // Save dialog handler - delegates to shared function
    ipcMain.handle('show-save-dialog', async () => {
        return showSaveDialog();
    });

    // Get last save path for test verification
    ipcMain.handle('get-last-save-path', () => {
        return lastSavePath;
    });
}

app.whenReady().then(() => {
    // Enable accessibility support for UI Automation integration
    app.accessibilitySupportEnabled = true;

    // Try setting specific accessibility features if available (Electron 29+)
    try {
        const features = ['nativeAPIs', 'webContents', 'inlineTextBoxes', 'extendedProperties', 'screenReader', 'html'];
        if (typeof (app as any).setAccessibilitySupportFeatures === 'function') {
            (app as any).setAccessibilitySupportFeatures(features);
            console.log('[A11y] Set accessibility features:', features);
        }
    } catch (e) {
        console.log('[A11y] setAccessibilitySupportFeatures not available, using legacy method');
    }

    console.log('[A11y] accessibilitySupportEnabled:', app.accessibilitySupportEnabled);

    // Setup IPC handlers before creating window
    setupIpcHandlers();

    createWindow();

    // Register Ctrl+S keyboard shortcut for Save using before-input-event
    // This captures the keystroke before the renderer processes it
    mainWindow?.webContents.on('before-input-event', (event, input) => {
        if (input.control && input.key.toLowerCase() === 's' && input.type === 'keyDown') {
            event.preventDefault();
            // Trigger save dialog directly
            showSaveDialog();
        }
    });

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow();
        }
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

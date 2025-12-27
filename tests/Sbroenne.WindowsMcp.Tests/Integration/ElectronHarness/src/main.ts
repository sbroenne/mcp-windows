import { app, BrowserWindow, ipcMain } from 'electron';
import * as path from 'path';

let mainWindow: BrowserWindow | null = null;

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

function setupIpcHandlers(): void {
    // IPC handlers for test verification
    ipcMain.handle('get-state', () => {
        // Return current application state for test verification
        return {
            isReady: true,
            windowTitle: mainWindow?.getTitle() ?? '',
        };
    });

    ipcMain.handle('reset-state', () => {
        // Reset application state
        mainWindow?.webContents.send('reset');
        return { success: true };
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

import { contextBridge, ipcRenderer } from 'electron';

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electronAPI', {
    getState: () => ipcRenderer.invoke('get-state'),
    resetState: () => ipcRenderer.invoke('reset-state'),
    showSaveDialog: () => ipcRenderer.invoke('show-save-dialog'),
    getLastSavePath: () => ipcRenderer.invoke('get-last-save-path'),
    onReset: (callback: () => void) => {
        ipcRenderer.on('reset', callback);
    },
    onStatusUpdate: (callback: (message: string) => void) => {
        ipcRenderer.on('status-update', (_event, message: string) => callback(message));
    },
});

// Type declaration for the exposed API
declare global {
    interface Window {
        electronAPI: {
            getState: () => Promise<{ isReady: boolean; windowTitle: string; lastSavePath: string | null }>;
            resetState: () => Promise<{ success: boolean }>;
            showSaveDialog: () => Promise<{ success: boolean; filePath?: string; cancelled?: boolean; error?: string }>;
            getLastSavePath: () => Promise<string | null>;
            onReset: (callback: () => void) => void;
            onStatusUpdate: (callback: (message: string) => void) => void;
        };
    }
}

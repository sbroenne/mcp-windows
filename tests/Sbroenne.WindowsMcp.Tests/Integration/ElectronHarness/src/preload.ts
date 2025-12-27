import { contextBridge, ipcRenderer } from 'electron';

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electronAPI', {
    getState: () => ipcRenderer.invoke('get-state'),
    resetState: () => ipcRenderer.invoke('reset-state'),
    onReset: (callback: () => void) => {
        ipcRenderer.on('reset', callback);
    },
});

// Type declaration for the exposed API
declare global {
    interface Window {
        electronAPI: {
            getState: () => Promise<{ isReady: boolean; windowTitle: string }>;
            resetState: () => Promise<{ success: boolean }>;
            onReset: (callback: () => void) => void;
        };
    }
}

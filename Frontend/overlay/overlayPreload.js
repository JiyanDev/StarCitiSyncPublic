const { contextBridge, ipcRenderer } = require('electron/renderer');

contextBridge.exposeInMainWorld('session', {
  onTimeUpdate: (callback) => {
    ipcRenderer.on('session:time', (_, timeStr) => {
      callback(timeStr);
    });
  },
  onLatestKills: (callback) => {
    ipcRenderer.on('session:latest-kills', (_, events) => {
      callback(events);
    });
  },
  onLatestStallActors: (callback) => {
  ipcRenderer.on('session:latest-stall-actors', (_, events) => {
    callback(events);
  });
}
});

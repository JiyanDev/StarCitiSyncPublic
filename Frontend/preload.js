const { contextBridge, ipcRenderer } = require('electron/renderer');

contextBridge.exposeInMainWorld('darkMode', {
  toggle: () => ipcRenderer.invoke('dark-mode:toggle'),
  system: () => ipcRenderer.invoke('dark-mode:system')
});

contextBridge.exposeInMainWorld('electronAPI', {
  quitApp: () => ipcRenderer.send('window-all-closed'),
  getAppVersion: () => ipcRenderer.invoke('get-app-version')
});

contextBridge.exposeInMainWorld('session', {
  onTimeUpdate: (callback) => ipcRenderer.on('session:time', (event, timeStr) => callback(timeStr)),
  onMissionCountUpdate: (callback) => ipcRenderer.on('session:missions', (event, count) => callback(count)),
  onLatestMission: (callback) => ipcRenderer.on('session:latest-mission', (event, mission) => callback(mission)),
  onRewardUpdate: (callback) => ipcRenderer.on('session:reward', (event, reward) => callback(reward)),
  onRewardLastHour: (callback) => ipcRenderer.on('session:reward-last-hour', (event, value) => callback(value)),
  onRewardPerHour: (callback) => ipcRenderer.on('session:reward-per-hour', (event, value) => callback(value)),
  getRewardGraph: () => ipcRenderer.invoke('get-reward-graph'),
  onShopSummary: (callback) => ipcRenderer.on('session:spendings', (event, spendings) => callback(spendings)),
  getCommodityProfitAndROI: () => ipcRenderer.invoke('get-commodity-profit-roi'),
});

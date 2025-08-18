const { app, BrowserWindow, ipcMain, nativeTheme, Tray, Menu, screen } = require('electron');
const path = require('node:path');
const fs = require('fs');
const { spawn } = require('child_process'); 
const {getCommodityProfitAndROIForSession, getLatestStallActors, updateAppClose, getdbPath, initDb, getLatestSession, getSessionById, getMissionCountForSession, getLatestUnfinishedMission, getTotalRewardForSession, getRewardLastHourForSession, getRewardPer10MinLastHour, getShopSummary, getLatestOverlayKillEvents} = require('./db');
const electronSquirrelStartup = require('electron-squirrel-startup');
const { checkForUpdates } = require('./updater');

if (electronSquirrelStartup) {
  // Squirrel startup event, do not continue
  app.quit();
}
const fetch = (...args) => import('node-fetch').then(({default: fetch}) => fetch(...args));
let appStartTime = Date.now();

let mainWindow;
let overlayWindow;
let currentSessionId = null;
let timer = null;
let startTime = null;
let tray = null;
let allowTimerStart = false;

function createWindow() {
  const primaryDisplay = screen.getPrimaryDisplay();
  const { width: screenWidth, height: screenHeight } = primaryDisplay.workArea;

  // Dynamic Size: 80% of screen, but at least 1100x800
  const winWidth = Math.max(Math.min(Math.round(screenWidth * 0.6), 1000), 1000);
  const winHeight = Math.max(Math.round(screenHeight * 0.8), 700) -100;

  mainWindow = new BrowserWindow({
    width: winWidth, //1200,
    height: winHeight, //980,
    minWidth: winWidth,
    minHeight: winHeight,
    icon: path.join(__dirname, '..', 'assets/citisync.ico'),
    titleBarStyle: 'hidden',
    frame: false,
    darkTheme: true,
    webPreferences: {
      preload: path.join(__dirname, '..', 'preloads', 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    }
  });

  mainWindow.on('close', () => {
    if (tray) {
      tray.destroy();
      tray = null;
    }
    if (overlayWindow && !overlayWindow.isDestroyed()) {
      overlayWindow.destroy();
      overlayWindow = null;
    }
  });

  mainWindow.loadFile('renderer/index.html');

  const { width, height } = primaryDisplay.workArea;
  overlayWindow = new BrowserWindow({
    width,
    height,
    x: 0,
    y: 0,
    frame: false,
    transparent: true,
    backgroundColor: '#00000000',  // Important for transparency
    alwaysOnTop: true,
    resizable: false,
    hasShadow: false,
    skipTaskbar: true,
    focusable: false,
    webPreferences: {
      preload: path.join(__dirname, '..', 'overlay', 'overlayPreload.js'),
      nodeIntegration: false,
      contextIsolation: true,
      backgroundThrottling: false
    }
  });
  overlayWindow.setIgnoreMouseEvents(true);
  overlayWindow.setAlwaysOnTop(true, 'screen-saver');
  overlayWindow.setBounds({ x: 0, y: 0, width, height });
  overlayWindow.loadFile(path.join(__dirname, '..', 'overlay', 'overlay.html'));
  overlayWindow.webContents.once('did-finish-load', () => {
    overlayWindow.show();
  });
}

app.on('browser-window-blur', () => {
  if (overlayWindow && !overlayWindow.isDestroyed()) {
    //console.log('🎮 Overlay update on focus');

    // Quick blink to force redraw
    overlayWindow.hide();
    setTimeout(() => {
      overlayWindow.show();
    }, 100);

    // use reload if above doesn't work
    // overlayWindow.reload();
  }
});
// IPC handler for dark mode toggle
ipcMain.handle('dark-mode:toggle', () => {
  //console.log('dark-mode:toggle received');
  nativeTheme.themeSource = nativeTheme.shouldUseDarkColors ? 'light' : 'dark';
  return nativeTheme.shouldUseDarkColors;
});

ipcMain.handle('dark-mode:system', () => {
  nativeTheme.themeSource = 'system';
});

ipcMain.handle('get-reward-graph', async () => {
  dbPath = getdbPath();
  if (!dbPath) {
    console.warn('DB not ready in get-reward-graph');
    return [];
  }
  const data = await getRewardPer10MinLastHour(currentSessionId);
  //console.log('get-reward-graph data:', data);
  return data; // [{ label, reward }, ...]
});

ipcMain.handle('get-commodity-profit-roi', async () => {
  if (!currentSessionId) return { profit: 0, roi: 0, totalBuy: 0, totalSell: 0 };
  return await getCommodityProfitAndROIForSession(currentSessionId);
});

ipcMain.handle('get-app-version', () => app.getVersion());

// Timer logic based on sessions in the database
async function checkSessionLoop() {
  try {
    const session = await getLatestSession();
    
    if (allowTimerStart && session && session.SessionId !== currentSessionId) {
      //console.log("🟢 new session found:", session.SessionId);
      currentSessionId = session.SessionId;
      startTime = new Date(session.StartDate);
      startTimer();
      pollLatestUnfinishedMission();
      pollTotalRewardForSession();
      pollRewardLastHour();
      pollSpendings();
      pollLatestOverlayKillEvents();
      pollLatestStallActors();
      allowTimerStart = false;
    }

    if (currentSessionId) {
      const sessionData = await getSessionById(currentSessionId);
      if (sessionData.EndDate) {
        //console.log("🔴 Session ended:", sessionData.EndDate);
        stopTimer();
        currentSessionId = null;
        allowTimerStart = false;
      }
    }
  } catch (err) {
    console.error("Fel i session-loop:", err.message);
  }

  setTimeout(checkSessionLoop, 2000); // Poll every 2 seconds
}

function startTimer() {
  //console.log("⏱️ Timer started:", startTime.toISOString());
  timer = setInterval(async () => {
    if (!startTime) return;
    const diff = Date.now() - startTime.getTime();
    const seconds = Math.floor(diff / 1000);
    const timeStr = formatTime(seconds);

    const missionCount = await getMissionCountForSession(currentSessionId);
    const reward = await getTotalRewardForSession(currentSessionId);
    const latestKills = await getLatestOverlayKillEvents(5);
    const hours = (Date.now() - startTime.getTime()) / 3600000;
    const rewardPerHour = hours > 0 ? reward / hours : 0;

    mainWindow.webContents.send('session:reward', reward);
    mainWindow.webContents.send('session:reward-per-hour', rewardPerHour);

    //console.log(`⏱️ sessiontime: ${timeStr}`);
    //console.log(`⏱️ ${timeStr} | Missions: ${missionCount}`);

    if (mainWindow) {
      mainWindow.webContents.send('session:time', timeStr);
      mainWindow.webContents.send('session:missions', missionCount);
    }

    if (overlayWindow) {
      overlayWindow.webContents.send('session:time', timeStr);
      overlayWindow.webContents.send('session:latest-kills', latestKills);
    }
  }, 1000);
}

async function pollLatestUnfinishedMission() {
  if (!currentSessionId || !mainWindow) return;

  try {
    const mission = await getLatestUnfinishedMission(currentSessionId);

    if (mission) {
      mainWindow.webContents.send('session:latest-mission', mission);
    }
  } catch (err) {
    console.error("❌ Error fetching mission:", err.message);
  }

  setTimeout(pollLatestUnfinishedMission, 3000);
}

async function pollTotalRewardForSession() {
  if (!currentSessionId || !mainWindow) return;

  try {
    const totalReward = await getTotalRewardForSession(currentSessionId);

    mainWindow.webContents.send('session:reward', totalReward);
  } catch (err) {
    console.error("❌ Error fetching total reward:", err.message);
  }

  setTimeout(pollTotalRewardForSession, 3000);
}

async function pollRewardLastHour() {
  if (!currentSessionId || !mainWindow) return;

  try {
    const rewardLastHour = await getRewardLastHourForSession(currentSessionId);
    mainWindow.webContents.send('session:reward-last-hour', rewardLastHour);
  } catch (err) {
    console.error("❌ Error fetching reward last hour:", err.message);
  }

  setTimeout(pollRewardLastHour, 60000); // every minute
}

async function pollSpendings() {
  if (!currentSessionId || !mainWindow) return;

  try {
    const spendings = await getShopSummary();
    mainWindow.webContents.send('session:spendings', spendings);
  } catch (err) {
    console.error("❌ Error fetching shop summary:", err.message);
  }

  setTimeout(pollSpendings, 60000); // every minute
}

async function pollLatestOverlayKillEvents() {
  if (!overlayWindow || overlayWindow.isDestroyed()) return;

  try {
    const events = await getLatestOverlayKillEvents(5);
    overlayWindow.webContents.send('session:latest-kills', events);
  } catch (err) {
    console.error("❌ Error fetching latest kill events:", err.message);
  }

  setTimeout(pollLatestOverlayKillEvents, 5000);
}

async function pollLatestStallActors() {
  if (!overlayWindow || overlayWindow.isDestroyed()) return;
  try {
    const events = await getLatestStallActors(currentSessionId, 10);
    overlayWindow.webContents.send('session:latest-stall-actors', events);
  } catch (err) {
    console.error("❌ Error fetching latest stall actors:", err.message);
  }
  setTimeout(pollLatestStallActors, 2000);
}

function stopTimer() {
  //console.log("🛑 Timer stopped");
  clearInterval(timer);
  timer = null;
  startTime = null;
}

function formatTime(seconds) {
  const h = Math.floor(seconds / 3600).toString().padStart(2, '0');
  const m = Math.floor((seconds % 3600) / 60).toString().padStart(2, '0');
  const s = (seconds % 60).toString().padStart(2, '0');
  return `${h}:${m}:${s}`;
}


let csharpProcess = null;
app.whenReady().then(() => {
  let exePath;
  if (app.isPackaged) {
     exePath = path.join(process.resourcesPath, 'backend', 'StarCitiSync.Client.exe');
  } else {
     exePath = path.join(__dirname, '..', 'resources', 'backend', 'StarCitiSync.Client.exe');
  }
    //csharpProcess = spawn('cmd.exe', ['/c', 'start', '', exePath]);
    csharpProcess = spawn(exePath, [], {
      detached: false,
      stdio: ['ignore', 'pipe', 'pipe']
    });

    csharpProcess.stdout.setEncoding('utf8');
    csharpProcess.stdout.on('data', (data) => {
      const lines = data.toString().split('\n');
      for (const line of lines) {
        if (line.startsWith('Log Start Date:')) {
          allowTimerStart = true;
        }
      }
    });

    setTimeout(() => {
      //console.log("5 second timeout");
    }, 5000);
   csharpProcess.on('error', (err) => {
     console.error("❌ Could not start the sync:", err.message);
   });

  let sessionLoopStarted = false;

  function waitForDatabaseAndStart() {
  const maxTries = 30;
  let tries = 0;
  let dbPath2 = getdbPath();
  function check() {
    if (fs.existsSync(dbPath2)) {
      if (!sessionLoopStarted) {
        sessionLoopStarted = true;
        initDb(dbPath2); 
        setTimeout(check, 3000);
        createWindow();
        checkSessionLoop();
      }
    } else if (tries < maxTries) {
      tries++;
      setTimeout(check, 5000);
      dbPath2 = getdbPath();
    } else {
      console.error('❌ Timeout: Database should never be missing!');
    }
  }
  check();
}

  waitForDatabaseAndStart();

  tray = new Tray(path.join(__dirname, '..', 'assets', 'logo2.png'));
  const trayMenu = Menu.buildFromTemplate([
    {
      label: 'Open',
      click: () => {
        if (!mainWindow || mainWindow.isDestroyed()) {
          createWindow();
        } else {
          if (mainWindow.isMinimized()) mainWindow.restore();
          mainWindow.show();
          mainWindow.focus();
        }
      }
    },
    { type: 'separator' },
    {
      label: 'Exit',
      click: () => {
        if (tray) {
          tray.destroy();
          tray = null;
        }
        app.quit();
      }
    }
  ]);
  tray.setContextMenu(trayMenu);

  if (app.isPackaged) {
    checkForUpdates();
  }
  
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});


let shuttingDown = false;

async function shutdownApp() {
  if (shuttingDown) return;
  shuttingDown = true;

  const durationMin = Math.round((Date.now() - appStartTime) / 60000);
  fetch('https://plausible.io/api/event', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      name: 'app_session_length',
      url: 'app://starcitisync',
      domain: 'starcitisyncpublic',
      props: { minutes: durationMin }
    })
  }).catch(err => {
    console.error('❌ failed:', err);
  });

  if (tray) { tray.destroy(); tray = null; }
  if (overlayWindow && !overlayWindow.isDestroyed()) {
    overlayWindow.destroy();
    overlayWindow = null;
  }
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.destroy();
    mainWindow = null;
  }

  if (csharpProcess && !csharpProcess.killed) {
    csharpProcess.kill();

    csharpProcess.once('exit', async () => {
      if (currentSessionId) {
        try {
          await updateAppClose(currentSessionId, new Date().toISOString());
        } catch (err) {
          console.error('❌ Failed to update AppClose:', err);
        }
      }
      app.quit();
    });

    setTimeout(async () => {
      if (currentSessionId) {
        try {
          await updateAppClose(currentSessionId, new Date().toISOString());
        } catch (err) {
          console.error('❌ Failed to update AppClose (timeout):', err);
        }
      }
      app.quit();
    }, 5000);

  } else {
    if (currentSessionId) {
      try {
        await updateAppClose(currentSessionId, new Date().toISOString());
      } catch (err) {
        console.error('❌ Failed to update AppClose:', err);
      }
    }
    app.quit();
  }
}

app.on('before-quit', shutdownApp);
app.on('window-all-closed', shutdownApp);
ipcMain.on('window-all-closed', shutdownApp);
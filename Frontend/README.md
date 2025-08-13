# StarCitiSync Frontend

**StarCitiSync** is a desktop application for tracking and visualizing statistics from your Star Citizen game sessions. The app displays session time, completed missions, rewards, spendings, and recent kills in real time—both in the main window and as a transparent overlay.

## Features

- **Automatic session tracking:** Detects and tracks your play sessions automatically.
- **Real-time statistics:** Displays session time, mission count, rewards, spendings, and more.
- **Overlay:** Transparent overlay showing timer and latest kills on top of your game.
- **Reward graph:** Visualizes rewards over time.
- **Spending summary:** Summarizes spendings, most purchased items, and top shop.
- **Auto-update:** Built-in update system via GitHub Releases.
- **Tray icon:** Quick access and control from the system tray.

## Installation

1. **Download** the latest release from [GitHub Releases](https://github.com/JiyanDev/StarCitiSyncPublic/releases).
2. **Install** by running the installer (`.exe`).
3. Launch StarCitiSync – overlay and statistics will appear automatically.

## Development

### Prerequisites

- [Node.js](https://nodejs.org/) (v18 or later recommended)
- [Git](https://git-scm.com/)
- [Electron](https://www.electronjs.org/)

### Getting Started

```sh
git clone https://github.com/JiyanDev/StarCitiSyncPublic.git
cd StarCitiSyncPublic
npm install
npm start
```

### Build and Package

Build a release with Electron Forge:

```sh
npm run make
```

## Project Structure

- [`main.js`](main.js) – Main process, window management, overlay, IPC.
- [`main/db.js`](main/db.js) – Database logic (SQLite), fetches stats and sessions.
- [`index.html`](index.html) – UI for statistics and graphs.
- [`overlay.html`](overlay.html) – Overlay UI for timer and kills.
- [`preload.js`](preload.js) / [`overlayPreload.js`](overlayPreload.js) – Secure bridge between renderer and main process.
- [`resources/backend/StarCitiSync.Client.exe`](resources/backend/StarCitiSync.Client.exe) – Background process that collects data from the game.

## Packaging & Distribution

Packaging and distribution are handled via [Electron Forge](https://www.electronforge.io/):

- Configuration in [`forge.config.js`](forge.config.js)
- Icons and resources in `assets/` and `resources/backend/`

## License

Apache-2.0 – see [LICENSE](LICENSE).

---

**Developer:** [JiyanDev](https://github.com/JiyanDev)

Feedback and bug reports are welcome via [issues](https://github.com/JiyanDev/StarCitiSyncPublic/issues)

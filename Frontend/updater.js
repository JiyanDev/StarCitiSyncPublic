const { app } = require('electron');
const { execFile } = require('child_process');
const path = require('path');
const https = require('https');

const GITHUB_OWNER = 'JiyanDev';
const GITHUB_REPO = 'StarCitiSyncPublic';

function getUpdateExePath() {
  return path.join(
    path.dirname(process.execPath),
    '..',
    'Update.exe'
  );
}

function compareVersions(v1, v2) {
  const a = v1.split('.').map(Number);
  const b = v2.split('.').map(Number);
  for (let i = 0; i < Math.max(a.length, b.length); i++) {
    const diff = (a[i] || 0) - (b[i] || 0);
    if (diff !== 0) return diff > 0 ? 1 : -1;
  }
  return 0;
}

function getLatestRelease(callback) {
  const options = {
    hostname: 'api.github.com',
    path: `/repos/${GITHUB_OWNER}/${GITHUB_REPO}/releases/latest`,
    headers: { 'User-Agent': `${GITHUB_REPO}-updater` }
  };

  https.get(options, res => {
    let data = '';
    res.on('data', chunk => data += chunk);
    res.on('end', () => {
      try {
        const release = JSON.parse(data);
        callback(null, release.tag_name.replace(/^v/, ''));
      } catch (err) {
        callback(err);
      }
    });
  }).on('error', callback);
}

function runUpdate(latestVersion) {
  const updateUrl = `https://github.com/${GITHUB_OWNER}/${GITHUB_REPO}/releases/download/v${latestVersion}/`;
  const updateExe = getUpdateExePath();

  console.log(`Running updater: ${updateExe} --update ${updateUrl}`);

  execFile(updateExe, ['--update', updateUrl], (error) => {
    if (error) {
      console.error('Update error:', error);
      return;
    }
    console.log('Update complete, restarting...');
    app.quit();
  });
}

function checkForUpdates() {
  const currentVersion = app.getVersion();

  getLatestRelease((err, latestVersion) => {
    if (err) {
      console.error('Could not fetch release info:', err);
      return;
    }

    console.log(`Current version: ${currentVersion}, latest: ${latestVersion}`);

    if (compareVersions(latestVersion, currentVersion) > 0) {
      console.log('Newer version found! Updating...');
      runUpdate(latestVersion);
    } else {
      console.log('No update needed.');
    }
  });
}

module.exports = { checkForUpdates };

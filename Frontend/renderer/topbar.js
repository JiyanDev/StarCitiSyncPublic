if (window.electronAPI && document.getElementById('version-link')) {
  window.electronAPI.getAppVersion().then(version => {
    const link = document.getElementById('version-link');
    link.innerText = `v${version}`;
    link.href = "https://github.com/JiyanDev/StarCitiSyncPublic/releases/latest";
    link.target = "_blank";
  });
}

const quitBtn = document.getElementById('quit-btn');
if (quitBtn && window.electronAPI) {
  quitBtn.addEventListener('click', () => {
    window.electronAPI.quitApp();
  });
}
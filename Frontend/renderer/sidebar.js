function initSidebar() {
  // Navigation logic for tabs
  if (document.getElementById('nav-home')) {
    document.getElementById('nav-home').onclick = () => window.location.href = 'index.html';
    document.getElementById('nav-kills').onclick = () => window.location.href = 'kills.html';
    document.getElementById('nav-trading').onclick = () => window.location.href = 'trading.html';
  }

  // Overlay toggles with persistent state
  const overlayTimerCheckbox = document.getElementById('toggle-overlay-timer');
  const playerStallCheckbox = document.getElementById('toggle-player-stall');

  function updateOverlayTimerVisibility(visible) {
    const overlayTimer = document.getElementById('overlay-timer');
    if (overlayTimer) {
      overlayTimer.style.display = visible ? '' : 'none';
    }
  }

  if (overlayTimerCheckbox && playerStallCheckbox) {
    overlayTimerCheckbox.checked = localStorage.getItem('showOverlayTimer') !== 'false';
    playerStallCheckbox.checked = localStorage.getItem('showPlayerStall') === 'true';

    updateOverlayTimerVisibility(overlayTimerCheckbox.checked);

    overlayTimerCheckbox.addEventListener('change', (e) => {
      localStorage.setItem('showOverlayTimer', e.target.checked);
      updateOverlayTimerVisibility(e.target.checked);
    });

    playerStallCheckbox.addEventListener('change', (e) => {
      localStorage.setItem('showPlayerStall', e.target.checked);
    });
  }

  // Side panel toggle logic
  if (document.querySelector('.menu-button')) {
    document.querySelector('.menu-button').addEventListener('click', () => {
      const sidePanel = document.getElementById('side-panel');
      if (sidePanel.style.left === '0px') {
        sidePanel.style.left = '-320px';
      } else {
        sidePanel.style.left = '0px';
      }
    });
  }

  // Optional: click outside to close
  document.addEventListener('mousedown', (e) => {
    const sidePanel = document.getElementById('side-panel');
    if (
      sidePanel &&
      sidePanel.style.left === '0px' &&
      !sidePanel.contains(e.target) &&
      !e.target.classList.contains('menu-button')
    ) {
      sidePanel.style.left = '-320px';
    }
  });

  // Show Latest Kills select and seconds input
  const latestKillsSelect = document.getElementById('toggle-latest-kills');
  const killsSecondsLabel = document.getElementById('latest-kills-seconds-label');
  const killsSecondsInput = document.getElementById('latest-kills-seconds');

  function updateSecondsVisibility() {
    if (killsSecondsLabel && latestKillsSelect) {
      killsSecondsLabel.style.display = latestKillsSelect.value === 'default' ? 'block' : 'none';
    }
  }

  if (latestKillsSelect) {
    latestKillsSelect.value = localStorage.getItem('latestKillsSetting') || 'default';
    updateSecondsVisibility();

    latestKillsSelect.addEventListener('change', (e) => {
      localStorage.setItem('latestKillsSetting', e.target.value);
      window.dispatchEvent(new StorageEvent('storage', { key: 'latestKillsSetting', newValue: e.target.value }));
      updateSecondsVisibility();
    });
  }

  if (killsSecondsInput) {
    killsSecondsInput.value = localStorage.getItem('latestKillsSeconds') || 6;
    killsSecondsInput.addEventListener('change', (e) => {
      let val = Math.max(2, Math.min(30, Number(e.target.value) || 6));
      killsSecondsInput.value = val;
      localStorage.setItem('latestKillsSeconds', val);
      window.dispatchEvent(new StorageEvent('storage', { key: 'latestKillsSeconds', newValue: val }));
    });
  }
}

// Call the function when the script is loaded
initSidebar();
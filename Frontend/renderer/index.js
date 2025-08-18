window.addEventListener('DOMContentLoaded', async () => {
  // Load topbar
  const topbarRes = await fetch('topbar.html');
  const topbarHtml = await topbarRes.text();
  const topbarTemp = document.createElement('div');
  topbarTemp.innerHTML = topbarHtml;
  const topbarTpl = topbarTemp.querySelector('template');
  if (topbarTpl) {
    document.body.prepend(topbarTpl.content.cloneNode(true));
  }

  // Load footer
  const footerRes = await fetch('footer.html');
  const footerHtml = await footerRes.text();
  const footerTemp = document.createElement('div');
  footerTemp.innerHTML = footerHtml;
  const footerTpl = footerTemp.querySelector('template');
  if (footerTpl) {
    document.getElementById('footer-root').appendChild(footerTpl.content.cloneNode(true));
  }

  // Load sidebar
  const sidebarRes = await fetch('sidebar.html');
  const sidebarHtml = await sidebarRes.text();
  const sidebarTemp = document.createElement('div');
  sidebarTemp.innerHTML = sidebarHtml;
  const sidebarTpl = sidebarTemp.querySelector('template');
  if (sidebarTpl) {
    document.body.appendChild(sidebarTpl.content.cloneNode(true));
  }

  // Now that sidebar is in DOM, load and run sidebar.js
  const sidebarScript = document.createElement('script');
  sidebarScript.src = 'sidebar.js';
  document.body.appendChild(sidebarScript);

  // Now that topbar is in DOM, load and run topbar.js
  const topbarScript = document.createElement('script');
  topbarScript.src = 'topbar.js';
  document.body.appendChild(topbarScript);

  // --- ALLA session/DOM-anrop här! ---
  if (window.session) {
    window.session.onTimeUpdate(timeStr => {
      document.getElementById('timer').innerText = timeStr;
    });

    window.session.onMissionCountUpdate(count => {
      document.getElementById('mission-count').innerText = count;
    });

    window.session.onLatestMission(mission => {
      const text = mission
        ? `${mission.Contract} (${mission.CompletionType || 'In Progress'})`
        : 'None Active';
      const el = document.getElementById('latest-mission');
      el.innerText = text;
      el.title = text;
    });

    window.session.onRewardUpdate(total => {
      document.getElementById('total-reward').innerText = `${total.toLocaleString()} UEC`;
    });

    window.session.onRewardLastHour(value => {
      document.getElementById('reward-last-hour').innerText = `${value.toLocaleString()} UEC`;
    });

    window.session.onRewardPerHour(value => {
      document.getElementById('reward-per-hour').innerText = `${Math.round(value).toLocaleString()} UEC/h`;
    });

    window.session.onShopSummary(spendings => {
      document.getElementById('total-spent').innerText =
        (spendings.TotalSpent != null
          ? spendings.TotalSpent.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
          : '0.00') + ' aUEC';
      document.getElementById('transaction-count').innerText = spendings.TotalTransactions ?? '0';
      document.getElementById('top-shop').innerText = spendings.TopShop ?? '–';
      document.getElementById('top-global-item').innerText = spendings.TopItem ?? '–';
    });

    function updateRewardGraph() {
      window.session.getRewardGraph().then(dataPoints => {
        rewardChart.data.labels = dataPoints.map(dp => dp.label);
        rewardChart.data.datasets[0].data = dataPoints.map(dp => dp.reward);
        rewardChart.update();
      });
    }

    function updateCommoditySummary() {
      window.session.getCommodityProfitAndROI().then(({ profit, roi, totalBuy, totalSell }) => {
        document.getElementById('commodity-profit').innerText =
          (profit != null ? profit.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '0.00') + ' aUEC';
        document.getElementById('commodity-roi').innerText =
          (roi != null ? roi.toFixed(1) : '0.0') + '%';
        document.getElementById('commodity-total-buy').innerText =
          (totalBuy != null ? totalBuy.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '0.00') + ' aUEC';
        document.getElementById('commodity-total-sell').innerText =
          (totalSell != null ? totalSell.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '0.00') + ' aUEC';
      });
    }
    updateCommoditySummary();
    setInterval(updateCommoditySummary, 10000);

    updateRewardGraph();
    setInterval(updateRewardGraph, 60000);
  }

  // Chart.js-setup (måste ligga efter DOM finns)
  function resizeCanvasToDisplaySize(canvas) {
    const dpr = window.devicePixelRatio || 1;
    const width = canvas.clientWidth * dpr;
    const height = (canvas.clientWidth / 3) * dpr;

    if (canvas.width !== width || canvas.height !== height) {
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext('2d');
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      return true;
    }
    return false;
  }

  const canvas = document.getElementById('rewardChart');
  resizeCanvasToDisplaySize(canvas);

  const ctx = document.getElementById('rewardChart').getContext('2d');
  const rewardChart = new Chart(ctx, {
    type: 'line',
    data: {
      labels: [],
      datasets: [{
        label: 'Reward Over Time',
        data: [],
        borderColor: '#ffb347',
        backgroundColor: 'rgba(255, 179, 71, 0.1)',
        borderWidth: 2,
        tension: 0.25,
        fill: true,
        pointRadius: 3
      }]
    },
    options: {
      scales: {
        x: {
          ticks: { color: '#ccc' },
          grid: { color: '#333' }
        },
        y: {
          beginAtZero: true,
          ticks: { color: '#ccc' },
          grid: { color: '#333' }
        }
      },
      plugins: {
        legend: { labels: { color: '#ccc' } }
      }
    }
  });

  window.addEventListener('resize', () => {
    const resized = resizeCanvasToDisplaySize(canvas);
    if (resized) {
      rewardChart.resize();
    }
    if (window.session) updateRewardGraph();
  });

  // Analytics
  function trackPlausible(eventName, props = {}) {
    fetch('https://plausible.io/api/event', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: eventName,
        url: 'app://starcitisync',
        domain: 'starcitisyncpublic',
        props
      })
    });
  }
  trackPlausible('app_open');
});
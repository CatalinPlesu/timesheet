const _charts = {};

export function renderLineChart(canvasId, labels, datasets, weekendIndices = []) {
  if (_charts[canvasId]) { _charts[canvasId].destroy(); }
  const ctx = document.getElementById(canvasId);
  if (!ctx) return;
  _charts[canvasId] = new Chart(ctx, {
    type: 'line',
    data: { labels, datasets },
    options: {
      responsive: true,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { position: 'top' },
        tooltip: {
          callbacks: {
            label: (ctx) => {
              const h = ctx.parsed.y;
              if (h === 0) return `${ctx.dataset.label}: 0m`;
              const totalMin = Math.round(h * 60);
              const hr = Math.floor(totalMin / 60);
              const mn = totalMin % 60;
              return `${ctx.dataset.label}: ${hr > 0 ? hr + 'h ' : ''}${mn}m`;
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          title: { display: true, text: 'Hours' },
          ticks: {
            callback: (v) => v === 0 ? '0' : `${Math.floor(v)}h`
          }
        },
        x: {
          ticks: {
            color: (ctx) => weekendIndices.includes(ctx.index) ? 'rgba(156,163,175,0.7)' : undefined
          }
        }
      }
    }
  });
}

export function renderStackedChart(canvasId, labels, workData, commuteData, lunchData, idleData) {
  if (_charts[canvasId]) { _charts[canvasId].destroy(); }
  const ctx = document.getElementById(canvasId);
  if (!ctx) return;
  _charts[canvasId] = new Chart(ctx, {
    type: 'bar',
    data: {
      labels,
      datasets: [
        { label: 'Work',    data: workData,    backgroundColor: 'rgba(59,130,246,0.8)' },
        { label: 'Commute', data: commuteData, backgroundColor: 'rgba(34,197,94,0.8)'  },
        { label: 'Lunch',   data: lunchData,   backgroundColor: 'rgba(251,146,60,0.8)' },
        { label: 'Idle',    data: idleData,    backgroundColor: 'rgba(156,163,175,0.4)' }
      ]
    },
    options: {
      responsive: true,
      plugins: { legend: { position: 'top' } },
      scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: true, title: { display: true, text: 'Hours' } } }
    }
  });
}

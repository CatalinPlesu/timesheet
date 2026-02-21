import { fetchStats, fetchChartData, fetchBreakdown } from '../api.js';
import { renderLineChart } from '../charts.js';

let anaPeriod = 30;
let anaTab = 'stats';

function fmtDur(h) {
  if (!h) return '0m';
  const m = Math.round(h * 60);
  const hr = Math.floor(m / 60), mn = m % 60;
  return hr > 0 ? `${hr}h ${mn}m` : `${mn}m`;
}

function dateRange(days) {
  const end = new Date();
  const start = new Date(end - days * 86400000);
  return { startDate: start.toISOString().slice(0,10), endDate: end.toISOString().slice(0,10) };
}

export async function renderAnalytics() {
  document.getElementById('app').innerHTML = `
    <main class="container">
      <h2>Analytics</h2>
      <p id="ana-error" class="error" style="display:none"></p>
      <div class="period-tabs" id="period-tabs"></div>
      <div class="ana-tabs" id="ana-tabs"></div>
      <div id="ana-content"><p aria-busy="true">Loading…</p></div>
    </main>
  `;
  renderPeriodTabs();
  renderAnaTabs();
  await loadAll();
}

function renderPeriodTabs() {
  const el = document.getElementById('period-tabs');
  if (!el) return;
  el.innerHTML = [7,30,90,365].map(d =>
    `<button class="btn-compact${anaPeriod===d?' btn-active':''}" onclick="setAnaPeriod(${d})">${d}d</button>`
  ).join('');
  window.setAnaPeriod = async (d) => { anaPeriod = d; renderPeriodTabs(); await loadAll(); };
}

function renderAnaTabs() {
  const el = document.getElementById('ana-tabs');
  if (!el) return;
  el.innerHTML = ['stats','chart','calendar'].map(t =>
    `<button class="ana-tab${anaTab===t?' active':''}" onclick="setAnaTab('${t}')">${t.charAt(0).toUpperCase()+t.slice(1)}</button>`
  ).join('');
  window.setAnaTab = async (t) => { anaTab = t; renderAnaTabs(); await loadTab(); };
}

let _stats = null, _chart = null, _breakdown = null;

async function loadAll() {
  const { startDate, endDate } = dateRange(anaPeriod);
  document.getElementById('ana-content').innerHTML = '<p aria-busy="true">Loading…</p>';
  [_stats, _chart, _breakdown] = await Promise.all([
    fetchStats(anaPeriod),
    fetchChartData(startDate, endDate),
    fetchBreakdown(startDate, endDate)
  ]);
  await loadTab();
}

async function loadTab() {
  const el = document.getElementById('ana-content');
  if (!el) return;
  if (anaTab === 'stats') renderStats(el);
  else if (anaTab === 'chart') renderChart(el);
  else renderCalendar(el);
}

function statsRow(label, obj) {
  if (!obj) return `<tr><td>${label}</td><td>—</td><td>—</td><td>—</td><td>—</td><td>—</td></tr>`;
  return `<tr>
    <td>${label}</td>
    <td>${fmtDur(obj.avg)}</td><td>${fmtDur(obj.min)}</td>
    <td>${fmtDur(obj.max)}</td><td>${fmtDur(obj.stdDev)}</td>
    <td>${fmtDur(obj.total)}</td>
  </tr>`;
}

function renderStats(el) {
  if (!_stats) { el.innerHTML = '<p>No stats yet.</p>'; return; }
  el.innerHTML = `
    <p class="muted-sm">${_stats.periodDays} days in period, ${_stats.daysWithData} days with data</p>
    <article>
      <table class="stats-table">
        <thead><tr><th>Metric</th><th>Avg</th><th>Min</th><th>Max</th><th>Std Dev</th><th>Total</th></tr></thead>
        <tbody>
          ${statsRow('Work', _stats.work)}
          ${statsRow('Commute →Work', _stats.commuteToWork)}
          ${statsRow('Commute →Home', _stats.commuteToHome)}
          ${statsRow('Lunch', _stats.lunch)}
        </tbody>
      </table>
    </article>`;
}

function renderChart(el) {
  if (!_chart) { el.innerHTML = '<p>No chart data yet.</p>'; return; }
  el.innerHTML = '<canvas id="lineChart"></canvas>';
  const datasets = [
    { label: 'Work',    data: _chart.workHours,    borderColor: 'rgb(59,130,246)',  backgroundColor: 'rgba(59,130,246,0.1)',  fill: true, tension: 0.3 },
    { label: 'Commute', data: _chart.commuteHours, borderColor: 'rgb(34,197,94)',   backgroundColor: 'rgba(34,197,94,0.1)',   fill: true, tension: 0.3 },
    { label: 'Lunch',   data: _chart.lunchHours,   borderColor: 'rgb(251,146,60)',  backgroundColor: 'rgba(251,146,60,0.1)',  fill: true, tension: 0.3 },
    { label: 'Idle',    data: _chart.idleHours,    borderColor: 'rgb(156,163,175)', borderDash: [5,5], fill: false, tension: 0.3 }
  ];
  renderLineChart('lineChart', _chart.labels, datasets);
}

function renderCalendar(el) {
  if (!_breakdown || !_breakdown.length) { el.innerHTML = '<p>No data for this period.</p>'; return; }
  const days = _breakdown.map(d => {
    const label = d.date?.slice(5,10) || '';
    return { label, w: d.workHours||0, c: (d.commuteToWorkHours||0)+(d.commuteToHomeHours||0), l: d.lunchHours||0, active: d.hasActivity };
  });
  el.innerHTML = `
    <article>
      <div class="cal-grid">
        ${['Mon','Tue','Wed','Thu','Fri','Sat','Sun'].map(h => `<div class="cal-header">${h}</div>`).join('')}
        ${days.map(d => `
          <div class="cal-day${d.active?'':' no-data'}">
            <div class="cal-day-num">${d.label}</div>
            ${d.active ? `
              ${d.w>0?'<div class="cal-bar cal-work"></div>':''}
              ${d.c>0?'<div class="cal-bar cal-commute"></div>':''}
              ${d.l>0?'<div class="cal-bar cal-lunch"></div>':''}
            ` : '-'}
          </div>`).join('')}
      </div>
    </article>`;
}

import { fetchStats, fetchChartData, fetchBreakdown, fetchCommutePatterns, fetchPeriodAggregate } from '../api.js';
import { renderLineChart } from '../charts.js';

let anaPeriod = 30;
let anaTab = 'stats';
let calWeekOffset = 0;  // 0 = current week, -1 = last week, etc.

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

function getWeekRange(offset) {
  const now = new Date();
  // Find Monday of current week
  const day = now.getUTCDay(); // 0=Sun
  const monday = new Date(now);
  monday.setUTCDate(now.getUTCDate() - (day === 0 ? 6 : day - 1) + offset * 7);
  monday.setUTCHours(0, 0, 0, 0);
  const sunday = new Date(monday);
  sunday.setUTCDate(monday.getUTCDate() + 6);
  return { monday, sunday };
}

function fmtWeekLabel(monday, sunday) {
  const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
  const ms = months[monday.getUTCMonth()];
  const me = months[sunday.getUTCMonth()];
  return `${ms} ${monday.getUTCDate()} – ${me} ${sunday.getUTCDate()}, ${sunday.getUTCFullYear()}`;
}

function fmtMonthDay(d) {
  const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
  return `${months[d.getUTCMonth()]} ${d.getUTCDate()}`;
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
  el.innerHTML = ['stats','chart','calendar','commute'].map(t =>
    `<button class="ana-tab${anaTab===t?' active':''}" onclick="setAnaTab('${t}')">${t.charAt(0).toUpperCase()+t.slice(1)}</button>`
  ).join('');
  window.setAnaTab = async (t) => { anaTab = t; renderAnaTabs(); await loadTab(); };
}

let _stats = null, _chart = null, _breakdown = null, _periodAggregate = null;

async function loadAll() {
  const { startDate, endDate } = dateRange(anaPeriod);
  document.getElementById('ana-content').innerHTML = '<p aria-busy="true">Loading…</p>';
  [_stats, _chart, _breakdown, _periodAggregate] = await Promise.all([
    fetchStats(anaPeriod),
    fetchChartData(startDate, endDate),
    fetchBreakdown(startDate, endDate),
    fetchPeriodAggregate(startDate, endDate)
  ]);
  await loadTab();
}

async function loadTab() {
  const el = document.getElementById('ana-content');
  if (!el) return;
  if (anaTab === 'stats') renderStats(el);
  else if (anaTab === 'chart') renderChart(el);
  else if (anaTab === 'calendar') await renderCalendar(el);
  else if (anaTab === 'commute') await renderCommute(el);
}

function median(arr) {
  if (!arr.length) return 0;
  const s = [...arr].sort((a, b) => a - b);
  const m = Math.floor(s.length / 2);
  return s.length % 2 ? s[m] : (s[m-1] + s[m]) / 2;
}

function statsRow(label, obj, med) {
  if (!obj) return `<tr><td>${label}</td><td>—</td><td>—</td><td>—</td><td>—</td><td>—</td><td>—</td></tr>`;
  return `<tr>
    <td>${label}</td>
    <td>${fmtDur(obj.avg)}</td>
    <td>${med !== undefined ? fmtDur(med) : '—'}</td>
    <td>${fmtDur(obj.min)}</td>
    <td>${fmtDur(obj.max)}</td>
    <td>${fmtDur(obj.stdDev)}</td>
    <td>${fmtDur(obj.total)}</td>
  </tr>`;
}

function renderStats(el) {
  if (!_stats) { el.innerHTML = '<p>No stats yet.</p>'; return; }

  // Compute medians from _breakdown
  const workValues = (_breakdown || []).map(d => d.workHours || 0).filter(v => v > 0);
  const workMedian = median(workValues);

  // Day-of-week breakdown from _breakdown
  const dowGroups = {};
  for (let i = 0; i < 7; i++) dowGroups[i] = { work: [], commute: [], lunch: [] };
  for (const d of (_breakdown || [])) {
    const dow = new Date(d.date).getUTCDay();
    dowGroups[dow].work.push(d.workHours || 0);
    dowGroups[dow].commute.push((d.commuteToWorkHours || 0) + (d.commuteToHomeHours || 0));
    dowGroups[dow].lunch.push(d.lunchHours || 0);
  }
  const dowNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  const dowRows = Object.entries(dowGroups).map(([dow, g]) => {
    const trips = g.work.filter(v => v > 0).length;
    if (trips === 0) return '';
    const avg = arr => arr.length ? arr.reduce((a, b) => a + b, 0) / arr.length : 0;
    return `<tr>
      <td>${dowNames[dow]}</td>
      <td>${fmtDur(avg(g.work))}</td>
      <td>${fmtDur(avg(g.commute))}</td>
      <td>${fmtDur(avg(g.lunch))}</td>
      <td>${trips}</td>
    </tr>`;
  }).join('');

  // Period totals
  let periodTotalsHtml = '';
  if (_periodAggregate) {
    const pa = _periodAggregate;
    periodTotalsHtml = `
      <article>
        <strong>Period Totals</strong>
        <p class="muted-sm" style="margin-top:0.5rem">
          Work: ${fmtDur(pa.totalWorkHours)}  &nbsp;|&nbsp;
          Commute: ${fmtDur(pa.totalCommuteHours)}  &nbsp;|&nbsp;
          Lunch: ${fmtDur(pa.totalLunchHours)}  &nbsp;|&nbsp;
          Work days: ${pa.workDaysCount ?? '—'}
        </p>
      </article>`;
  }

  el.innerHTML = `
    <p class="muted-sm">${_stats.periodDays} days in period, ${_stats.daysWithData} days with data</p>
    <article>
      <table class="stats-table">
        <thead><tr><th>Metric</th><th>Avg</th><th>Median</th><th>Min</th><th>Max</th><th>Std Dev</th><th>Total</th></tr></thead>
        <tbody>
          ${statsRow('Work', _stats.work, workMedian)}
          ${statsRow('Commute →Work', _stats.commuteToWork)}
          ${statsRow('Commute →Home', _stats.commuteToHome)}
          ${statsRow('Lunch', _stats.lunch)}
        </tbody>
      </table>
    </article>
    ${periodTotalsHtml}
    ${dowRows ? `
    <article>
      <strong>By Day of Week</strong>
      <table class="stats-table" style="margin-top:0.75rem">
        <thead><tr><th>Day</th><th>Avg Work</th><th>Avg Commute</th><th>Avg Lunch</th><th>Trips</th></tr></thead>
        <tbody>${dowRows}</tbody>
      </table>
    </article>` : ''}`;
}

function renderChart(el) {
  if (!_chart) { el.innerHTML = '<p>No chart data yet.</p>'; return; }
  el.innerHTML = '<canvas id="lineChart"></canvas>';
  const datasets = [
    { label: 'Work',      data: _chart.workHours,    borderColor: 'rgb(59,130,246)',  backgroundColor: 'rgba(59,130,246,0.1)',  fill: true, tension: 0.3 },
    { label: 'Commute',   data: _chart.commuteHours, borderColor: 'rgb(34,197,94)',   backgroundColor: 'rgba(34,197,94,0.1)',   fill: true, tension: 0.3 },
    { label: 'Lunch',     data: _chart.lunchHours,   borderColor: 'rgb(251,146,60)',  backgroundColor: 'rgba(251,146,60,0.1)',  fill: true, tension: 0.3 },
    { label: 'Idle',      data: _chart.idleHours,    borderColor: 'rgb(156,163,175)', borderDash: [5,5], fill: false, tension: 0.3 },
    {
      label: '8h target',
      data: _chart.labels.map(() => 8),
      borderColor: 'rgba(239,68,68,0.7)',
      borderDash: [8, 4],
      borderWidth: 1.5,
      pointRadius: 0,
      fill: false,
      tension: 0
    }
  ];
  renderLineChart('lineChart', _chart.labels, datasets);
}

async function renderCalendar(el) {
  el.innerHTML = '<p aria-busy="true">Loading week…</p>';
  const { monday, sunday } = getWeekRange(calWeekOffset);
  const startDate = monday.toISOString().slice(0, 10);
  const endDate = sunday.toISOString().slice(0, 10);

  let weekData = null;
  try {
    weekData = await fetchBreakdown(startDate, endDate);
  } catch {
    el.innerHTML = '<p class="error">Failed to load calendar data.</p>';
    return;
  }

  // Build a map from date string to breakdown entry
  const byDate = {};
  for (const d of (weekData || [])) {
    byDate[d.date?.slice(0, 10)] = d;
  }

  // Build 7 day cells Mon–Sun
  const dayCells = [];
  for (let i = 0; i < 7; i++) {
    const d = new Date(monday);
    d.setUTCDate(monday.getUTCDate() + i);
    const dateStr = d.toISOString().slice(0, 10);
    const entry = byDate[dateStr];
    const hasData = entry && entry.hasActivity;
    const w = entry ? (entry.workHours || 0) : 0;
    const c = entry ? ((entry.commuteToWorkHours || 0) + (entry.commuteToHomeHours || 0)) : 0;
    const l = entry ? (entry.lunchHours || 0) : 0;

    dayCells.push(`
      <div class="cal-day-card${hasData ? '' : ' no-data'}">
        <div class="cal-day-label">${fmtMonthDay(d)}</div>
        ${hasData ? `
          ${w > 0 ? `<div class="cal-activity-bar"><span class="cal-dot cal-work"></span> Work: ${fmtDur(w)}</div>` : ''}
          ${c > 0 ? `<div class="cal-activity-bar"><span class="cal-dot cal-commute"></span> Commute: ${fmtDur(c)}</div>` : ''}
          ${l > 0 ? `<div class="cal-activity-bar"><span class="cal-dot cal-lunch"></span> Lunch: ${fmtDur(l)}</div>` : ''}
        ` : '<div class="cal-no-data">—</div>'}
      </div>`);
  }

  el.innerHTML = `
    <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:1rem;">
      <button class="outline btn-compact" onclick="calPrev()">← Prev</button>
      <strong id="cal-week-label">${fmtWeekLabel(monday, sunday)}</strong>
      <button class="outline btn-compact" onclick="calNext()">Next →</button>
    </div>
    <div class="cal-week-grid">
      <div class="cal-col-header">Mon</div>
      <div class="cal-col-header">Tue</div>
      <div class="cal-col-header">Wed</div>
      <div class="cal-col-header">Thu</div>
      <div class="cal-col-header">Fri</div>
      <div class="cal-col-header">Sat</div>
      <div class="cal-col-header">Sun</div>
      ${dayCells.join('')}
    </div>`;

  window.calPrev = async () => { calWeekOffset--; await renderCalendar(document.getElementById('ana-content')); };
  window.calNext = async () => { calWeekOffset++; await renderCalendar(document.getElementById('ana-content')); };
}

async function renderCommute(el) {
  el.innerHTML = '<p aria-busy="true">Loading commute patterns…</p>';
  let toWork = null, toHome = null;
  try {
    [toWork, toHome] = await Promise.all([
      fetchCommutePatterns('ToWork'),
      fetchCommutePatterns('ToHome')
    ]);
  } catch {
    el.innerHTML = '<p class="error">Failed to load commute patterns.</p>';
    return;
  }

  const dowNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];

  function commuteTable(data) {
    if (!data || !data.length) return '<p class="muted-sm">No data yet.</p>';
    return `<table class="stats-table">
      <thead><tr><th>Day</th><th>Avg Commute</th><th>Best departure</th><th>Shortest commute</th><th>Trips</th></tr></thead>
      <tbody>
        ${data.map(r => `<tr>
          <td>${dowNames[r.dayOfWeek] || r.dayOfWeek}</td>
          <td>${fmtDur(r.averageDurationHours)}</td>
          <td>${String(Math.floor(r.optimalStartHour)).padStart(2,'0')}:00</td>
          <td>${fmtDur(r.shortestDurationHours)}</td>
          <td>${r.sessionCount}</td>
        </tr>`).join('')}
      </tbody>
    </table>`;
  }

  el.innerHTML = `
    <article>
      <strong>To Work</strong>
      ${commuteTable(toWork)}
    </article>
    <article>
      <strong>To Home</strong>
      ${commuteTable(toHome)}
    </article>`;
}

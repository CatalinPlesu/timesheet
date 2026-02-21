import { fetchStats, fetchChartData, fetchBreakdown, fetchCommutePatterns, fetchPeriodAggregate, fetchEntriesForRange } from '../api.js';
import { renderLineChart } from '../charts.js';

let anaPeriod = 30;
let anaTab = 'stats';
let calStartDate = null;  // Date object — start of visible window

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

function visibleDays() {
  return window.innerWidth < 640 ? 1 : window.innerWidth < 1024 ? 3 : 5;
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
  const labels = { 7: '7d', 30: '30d', 90: '90d', 365: '1y', 3650: 'All' };
  el.innerHTML = [7,30,90,365,3650].map(d =>
    `<button class="btn-compact${anaPeriod===d?' btn-active':''}" onclick="setAnaPeriod(${d})">${labels[d]}</button>`
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
  else if (anaTab === 'calendar') await renderCalendarTab(el);
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

  // Compute medians from _breakdown — exclude off-days (zero work hours)
  const workValues = (_breakdown || [])
    .map(d => d.workHours || 0)
    .filter(h => h > 0);
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

  const periodLabel = _stats ? (
    anaPeriod === 3650
      ? `All time · ${_stats.daysWithData} days with data`
      : `${_stats.periodDays} days in period · ${_stats.daysWithData} days with data`
  ) : '';

  el.innerHTML = `
    <p class="muted-sm">${periodLabel}</p>
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

// ─── Calendar: hourly timeline ────────────────────────────────────────────────

const pxPerMin = 1.5;
const defaultHourMin = 6;
const defaultHourMax = 20;

function addDays(date, n) {
  const d = new Date(date);
  d.setUTCDate(d.getUTCDate() + n);
  return d;
}

function isoDate(d) {
  return d.toISOString().slice(0, 10);
}

function fmtRangeLabel(start, n) {
  const dayNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
  if (n === 1) {
    return `${dayNames[start.getUTCDay()]} ${months[start.getUTCMonth()]} ${start.getUTCDate()}, ${start.getUTCFullYear()}`;
  }
  const end = addDays(start, n - 1);
  return `${dayNames[start.getUTCDay()]} ${months[start.getUTCMonth()]} ${start.getUTCDate()} – ${dayNames[end.getUTCDay()]} ${months[end.getUTCMonth()]} ${end.getUTCDate()}, ${end.getUTCFullYear()}`;
}

function sessionClass(state) {
  if (!state) return 'tl-work';
  const s = state.toLowerCase();
  if (s === 'working') return 'tl-work';
  if (s === 'commuting') return 'tl-commute';
  if (s === 'lunch') return 'tl-lunch';
  return 'tl-work';
}

function sessionLabel(state) {
  if (!state) return 'Working';
  const s = state.toLowerCase();
  if (s === 'working') return 'Working';
  if (s === 'commuting') return 'Commuting';
  if (s === 'lunch') return 'Lunch';
  return state;
}

export async function renderCalendarTab(el) {
  if (!el) el = document.getElementById('ana-content');
  if (!el) return;

  const n = visibleDays();

  // Initialize calStartDate to today if not set
  if (!calStartDate) {
    const now = new Date();
    calStartDate = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
    // Shift back so today is the last visible column
    calStartDate = addDays(calStartDate, -(n - 1));
  }

  el.innerHTML = '<p aria-busy="true">Loading timeline…</p>';

  // Fetch entries for the visible range
  const rangeStart = isoDate(calStartDate);
  const rangeEnd = isoDate(addDays(calStartDate, n));  // exclusive end
  let data = null;
  try {
    data = await fetchEntriesForRange(rangeStart, rangeEnd);
  } catch {
    el.innerHTML = '<p class="error">Failed to load calendar data.</p>';
    return;
  }

  const entries = (data && data.entries) ? data.entries : [];

  // Determine hour range from entries
  let hourMin = defaultHourMin;
  let hourMax = defaultHourMax;
  for (const e of entries) {
    const start = new Date(e.startedAt);
    const h1 = start.getUTCHours() + start.getUTCMinutes() / 60;
    hourMin = Math.min(hourMin, Math.floor(h1) - 1);
    if (e.endedAt) {
      const end = new Date(e.endedAt);
      const h2 = end.getUTCHours() + end.getUTCMinutes() / 60;
      hourMax = Math.max(hourMax, Math.ceil(h2) + 1);
    }
  }
  hourMin = Math.max(0, Math.min(hourMin, defaultHourMin));
  hourMax = Math.min(24, Math.max(hourMax, defaultHourMax));
  const totalHours = hourMax - hourMin;
  const totalPx = totalHours * pxPerMin * 60;

  // Build time column
  const hourLabels = [];
  for (let h = hourMin; h < hourMax; h++) {
    hourLabels.push(`${String(h).padStart(2, '0')}:00`);
  }

  // Build day columns
  const dayNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

  const dayCols = [];
  for (let i = 0; i < n; i++) {
    const colDate = addDays(calStartDate, i);
    const colDateStr = isoDate(colDate);

    // Filter entries for this day
    const dayEntries = entries.filter(e => e.startedAt && e.startedAt.slice(0, 10) === colDateStr);

    // Build hour grid lines
    const hourLines = hourLabels.map((_, idx) =>
      `<div class="tl-hour-line" style="top:${idx * pxPerMin * 60}px"></div>`
    ).join('');

    // Build session blocks
    const dayStartMs = Date.UTC(colDate.getUTCFullYear(), colDate.getUTCMonth(), colDate.getUTCDate(), hourMin, 0, 0, 0);

    const sessionBlocks = dayEntries.map(e => {
      const sessionStart = new Date(e.startedAt);
      const sessionEnd = e.endedAt ? new Date(e.endedAt) : new Date();
      const topPx = Math.max(0, (sessionStart.getTime() - dayStartMs) / 60000 * pxPerMin);
      const heightPx = Math.max((sessionEnd.getTime() - sessionStart.getTime()) / 60000 * pxPerMin, pxPerMin * 15);
      const cls = sessionClass(e.state);
      const activeCls = e.isActive ? ' tl-session-active' : '';
      const dur = e.durationHours != null ? fmtDur(e.durationHours) :
        fmtDur((sessionEnd - sessionStart) / 3600000);
      const label = heightPx >= 40
        ? `${sessionLabel(e.state)}<br><small>${dur}</small>`
        : '';
      return `<div class="tl-session ${cls}${activeCls}" style="top:${topPx.toFixed(1)}px; height:${heightPx.toFixed(1)}px" title="${sessionLabel(e.state)} — ${dur}">${label}</div>`;
    }).join('');

    dayCols.push(`
      <div class="tl-day-col">
        <div class="tl-day-header">${dayNames[colDate.getUTCDay()]}<br><small>${months[colDate.getUTCMonth()]} ${colDate.getUTCDate()}</small></div>
        <div class="tl-day-body" style="position:relative; height:${totalPx}px">
          ${hourLines}
          ${sessionBlocks}
        </div>
      </div>`);
  }

  // Time labels column
  const timeLabels = hourLabels.map(h =>
    `<div class="tl-hour" style="height:${pxPerMin * 60}px">${h}</div>`
  ).join('');

  const rangeLabel = fmtRangeLabel(calStartDate, n);

  el.innerHTML = `
    <div class="tl-container">
      <div class="tl-nav">
        <button class="outline btn-compact" onclick="calPrev()">←</button>
        <span id="tl-range-label">${rangeLabel}</span>
        <button class="outline btn-compact" onclick="calNext()">→</button>
        <button class="outline btn-compact secondary" onclick="calToday()">Today</button>
      </div>
      <div class="tl-grid" id="tl-grid" style="grid-template-columns: 52px repeat(${n}, 1fr)">
        <div class="tl-time-col">
          <div class="tl-corner"></div>
          ${timeLabels}
        </div>
        ${dayCols.join('')}
      </div>
    </div>`;

  window.calPrev = async () => {
    calStartDate = addDays(calStartDate, -visibleDays());
    await renderCalendarTab(document.getElementById('ana-content'));
  };
  window.calNext = async () => {
    calStartDate = addDays(calStartDate, visibleDays());
    await renderCalendarTab(document.getElementById('ana-content'));
  };
  window.calToday = async () => {
    calStartDate = null;
    await renderCalendarTab(document.getElementById('ana-content'));
  };
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

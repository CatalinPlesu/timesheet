import { fetchStats, fetchChartData, fetchBreakdown, fetchCommutePatterns, fetchPeriodAggregate, fetchEntriesForRange } from '../api.js';
import { renderLineChart } from '../charts.js';
import { toLocal, localDateISO } from '../time.js';

let anaPeriod = 30;
let anaTab = 'stats';
let calStartDate = null;  // Date object — start of visible window
let chartWindowStart = null; // Date — start of 14-day window
let chartType = 'bar';

function fmtDur(h) {
  if (!h) return '0m';
  const m = Math.round(h * 60);
  const hr = Math.floor(m / 60), mn = m % 60;
  if (hr > 0 && mn > 0) return `${hr}h ${mn}m`;
  if (hr > 0) return `${hr}h`;
  return `${mn}m`;
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
  window.setAnaPeriod = async (d) => {
    anaPeriod = d;
    chartWindowStart = null;
    renderPeriodTabs();
    await loadAll();
  };
}

function renderAnaTabs() {
  const el = document.getElementById('ana-tabs');
  if (!el) return;
  el.innerHTML = ['stats','chart','calendar','commute','patterns'].map(t =>
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
  else if (anaTab === 'patterns') await renderPatternsTab(el);
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

  const commuteWorkValues = (_breakdown || []).map(d => d.commuteToWorkHours || 0).filter(h => h > 0);
  const commuteWorkMedian = median(commuteWorkValues);
  const commuteHomeValues = (_breakdown || []).map(d => d.commuteToHomeHours || 0).filter(h => h > 0);
  const commuteHomeMedian = median(commuteHomeValues);
  const lunchValues = (_breakdown || []).map(d => d.lunchHours || 0).filter(h => h > 0);
  const lunchMedian = median(lunchValues);

  // Day-of-week breakdown from _breakdown
  const dowGroups = {};
  for (let i = 0; i < 7; i++) dowGroups[i] = { work: [], commute: [], lunch: [] };
  for (const d of (_breakdown || [])) {
    if (!d.workHours || d.workHours <= 0) continue; // skip non-work days
    const dow = new Date(d.date + 'T12:00:00Z').getUTCDay();
    dowGroups[dow].work.push(d.workHours);
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
          ${statsRow('Commute →Work', _stats.commuteToWork, commuteWorkMedian)}
          ${statsRow('Commute →Home', _stats.commuteToHome, commuteHomeMedian)}
          ${statsRow('Lunch', _stats.lunch, lunchMedian)}
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
  // Initialize window to last 14 days
  if (!chartWindowStart) {
    const now = new Date();
    chartWindowStart = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - 13));
  }
  const windowEnd = new Date(chartWindowStart);
  windowEnd.setUTCDate(windowEnd.getUTCDate() + 14);

  const startStr = chartWindowStart.toISOString().slice(0, 10);
  const endStr   = windowEnd.toISOString().slice(0, 10);

  // Show loading + nav
  el.innerHTML = `
    <div style="display:flex;gap:0.5rem;align-items:center;margin-bottom:0.75rem;flex-wrap:wrap;">
      <button class="outline btn-compact" id="chartPrev">← Prev</button>
      <strong style="flex:1;text-align:center" id="chartLabel">${startStr} – ${new Date(windowEnd - 1).toISOString().slice(0,10)}</strong>
      <button class="outline btn-compact" id="chartNext">Next →</button>
      <button class="outline btn-compact${chartType==='bar'?' secondary':''}" id="chartBarBtn">Bar</button>
      <button class="outline btn-compact${chartType==='line'?' secondary':''}" id="chartLineBtn">Line</button>
    </div>
    <canvas id="lineChart"></canvas>`;

  document.getElementById('chartPrev').onclick = async () => {
    chartWindowStart = new Date(chartWindowStart);
    chartWindowStart.setUTCDate(chartWindowStart.getUTCDate() - 14);
    renderChart(el);
  };
  document.getElementById('chartNext').onclick = async () => {
    chartWindowStart = new Date(chartWindowStart);
    chartWindowStart.setUTCDate(chartWindowStart.getUTCDate() + 14);
    renderChart(el);
  };
  document.getElementById('chartBarBtn').onclick = () => { chartType = 'bar'; renderChart(el); };
  document.getElementById('chartLineBtn').onclick = () => { chartType = 'line'; renderChart(el); };

  // Fetch and render async
  (async () => {
    try {
      const chartData = await fetchChartData(startStr, endStr);

      // Build a map from the API response
      const apiMap = {};
      if (chartData && chartData.labels) {
        chartData.labels.forEach((lbl, i) => {
          apiMap[lbl] = {
            work:    chartData.workHours?.[i]    ?? 0,
            commute: chartData.commuteHours?.[i] ?? 0,
            lunch:   chartData.lunchHours?.[i]   ?? 0,
            idle:    chartData.idleHours?.[i]     ?? 0,
          };
        });
      }

      // Fill every day in range
      const labels = [], workD = [], commuteD = [], lunchD = [], idleD = [];
      const weekendIndices = [];
      const cur = new Date(chartWindowStart);
      while (cur < windowEnd) {
        const ds = cur.toISOString().slice(0, 10);
        const dow = cur.getUTCDay();
        labels.push(ds.slice(5)); // "MM-DD"
        const d = apiMap[ds] || { work: 0, commute: 0, lunch: 0, idle: 0 };
        workD.push(d.work); commuteD.push(d.commute); lunchD.push(d.lunch); idleD.push(d.idle);
        if (dow === 0 || dow === 6) weekendIndices.push(labels.length - 1);
        cur.setUTCDate(cur.getUTCDate() + 1);
      }

      if (chartType === 'bar') {
        const canvas = document.getElementById('lineChart');
        if (!canvas) return;
        if (canvas._chartInstance) canvas._chartInstance.destroy();
        canvas._chartInstance = new Chart(canvas, {
          type: 'bar',
          data: {
            labels,
            datasets: [
              { label: 'Work',    data: workD,    backgroundColor: 'rgba(59,130,246,0.8)' },
              { label: 'Commute', data: commuteD, backgroundColor: 'rgba(34,197,94,0.8)' },
              { label: 'Lunch',   data: lunchD,   backgroundColor: 'rgba(251,146,60,0.8)' },
              { label: 'Idle',    data: idleD,    backgroundColor: 'rgba(156,163,175,0.5)' },
            ]
          },
          options: {
            responsive: true,
            plugins: {
              legend: { position: 'top' },
              tooltip: { callbacks: { label: (ctx) => `${ctx.dataset.label}: ${fmtDur(ctx.parsed.y)}` } }
            },
            scales: {
              x: {
                ticks: {
                  color: (ctx) => weekendIndices.includes(ctx.index) ? 'rgba(156,163,175,0.6)' : undefined
                }
              },
              y: {
                beginAtZero: true,
                ticks: { callback: v => v === 0 ? '0' : `${Math.floor(v)}h` }
              }
            }
          }
        });
      } else {
        const datasets = [
          { label: 'Work',    data: workD,    borderColor: 'rgb(59,130,246)',  backgroundColor: 'rgba(59,130,246,0.1)', fill: true, tension: 0.3 },
          { label: 'Commute', data: commuteD, borderColor: 'rgb(34,197,94)',   backgroundColor: 'rgba(34,197,94,0.1)',  fill: true, tension: 0.3 },
          { label: 'Lunch',   data: lunchD,   borderColor: 'rgb(251,146,60)',  backgroundColor: 'rgba(251,146,60,0.1)', fill: true, tension: 0.3 },
          { label: 'Idle',    data: idleD,    borderColor: 'rgb(156,163,175)', borderDash: [5,5], fill: false, tension: 0.3 },
          { label: '8h target', data: labels.map(() => 8), borderColor: 'rgba(239,68,68,0.7)', borderDash: [8,4], borderWidth: 1.5, pointRadius: 0, fill: false }
        ];
        renderLineChart('lineChart', labels, datasets, weekendIndices);
      }
    } catch {
      el.innerHTML += '<p class="error">Failed to load chart.</p>';
    }
  })();
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

function fmtRangeLabel(start, end) {
  const dayNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
  if (isoDate(start) === isoDate(end)) {
    return `${dayNames[start.getUTCDay()]} ${months[start.getUTCMonth()]} ${start.getUTCDate()}, ${start.getUTCFullYear()}`;
  }
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

// Calendar weekday helpers
function isWeekend(date) { const d = date.getUTCDay(); return d === 0 || d === 6; }

function getMondayOfWeek(date) {
  const d = new Date(date);
  const dow = d.getUTCDay();
  const diff = dow === 0 ? -6 : 1 - dow; // Monday
  d.setUTCDate(d.getUTCDate() + diff);
  return d;
}

function getFridayOfWeek(date) {
  const mon = getMondayOfWeek(date);
  const fri = new Date(mon);
  fri.setUTCDate(mon.getUTCDate() + 4);
  return fri;
}

export async function renderCalendarTab(el) {
  if (!el) el = document.getElementById('ana-content');
  if (!el) return;

  const n = visibleDays();

  // Initialize calStartDate to Monday of current week if not set
  if (!calStartDate) {
    calStartDate = getMondayOfWeek(new Date());
  }

  el.innerHTML = '<p aria-busy="true">Loading timeline…</p>';

  // Build weekday dates array (skip weekends)
  const weekdayDates = [];
  let cur = new Date(calStartDate);
  while (weekdayDates.length < n) {
    if (!isWeekend(cur)) weekdayDates.push(new Date(cur));
    cur.setUTCDate(cur.getUTCDate() + 1);
  }

  // Fetch entries for the visible range
  const rangeStart = isoDate(calStartDate);
  const rangeEnd = isoDate(addDays(weekdayDates[weekdayDates.length - 1], 1));
  let data = null;
  try {
    data = await fetchEntriesForRange(rangeStart, rangeEnd);
  } catch {
    el.innerHTML = '<p class="error">Failed to load calendar data.</p>';
    return;
  }

  const entries = (data && data.entries) ? data.entries : [];

  // Determine hour range from entries (using local times)
  let hourMin = defaultHourMin;
  let hourMax = defaultHourMax;
  for (const e of entries) {
    const localStart = toLocal(e.startedAt);
    if (!localStart) continue;
    const h1 = localStart.getHours() + localStart.getMinutes() / 60;
    hourMin = Math.min(hourMin, Math.floor(h1) - 1);
    if (e.endedAt) {
      const localEnd = toLocal(e.endedAt);
      const h2 = localEnd.getHours() + localEnd.getMinutes() / 60;
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
  for (let i = 0; i < weekdayDates.length; i++) {
    const colDate = weekdayDates[i];
    const colDateStr = isoDate(colDate);

    // Filter entries for this day using local date
    const dayEntries = entries.filter(e => e.startedAt && localDateISO(e.startedAt) === colDateStr);

    // Build hour grid lines
    const hourLines = hourLabels.map((_, idx) =>
      `<div class="tl-hour-line" style="top:${idx * pxPerMin * 60}px"></div>`
    ).join('');

    // local midnight of colDate in ms
    const localMidnightMs = new Date(colDateStr + 'T00:00:00').getTime(); // local midnight, no Z
    const dayStartMs = localMidnightMs + hourMin * 3600000;

    const fmtTime = (d) => `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`;
    const sessionBlocks = dayEntries.map(e => {
      const sessionStart = toLocal(e.startedAt);
      const sessionEnd   = e.endedAt ? toLocal(e.endedAt) : toLocal(new Date().toISOString());
      const topPx = Math.max(0, (sessionStart.getTime() - dayStartMs) / 60000 * pxPerMin);
      const heightPx = Math.max((sessionEnd.getTime() - sessionStart.getTime()) / 60000 * pxPerMin, pxPerMin * 15);
      const cls = sessionClass(e.state);
      const activeCls = e.isActive ? ' tl-session-active' : '';
      const dur = e.durationHours != null ? fmtDur(e.durationHours) :
        fmtDur((sessionEnd - sessionStart) / 3600000);
      const notePreview = (heightPx >= 60 && e.note)
        ? `<br><small><em>${e.note.substring(0, 30)}${e.note.length > 30 ? '…' : ''}</em></small>`
        : '';
      const label = heightPx >= 40
        ? `${sessionLabel(e.state)}<br><small>${dur}</small>${notePreview}`
        : '';
      const startTimeStr = fmtTime(sessionStart);
      const endTimeStr = e.endedAt ? fmtTime(sessionEnd) : 'now';
      const noteText = e.note ? `\nNote: ${e.note}` : '';
      const titleText = `${sessionLabel(e.state)}\n${startTimeStr} – ${endTimeStr} (${dur})${noteText}`;
      return `<div class="tl-session ${cls}${activeCls}" style="top:${topPx.toFixed(1)}px; height:${heightPx.toFixed(1)}px" title="${titleText.replace(/"/g, '&quot;')}">${label}</div>`;
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

  const lastDate = weekdayDates[weekdayDates.length - 1];
  const rangeLabel = fmtRangeLabel(calStartDate, lastDate);

  el.innerHTML = `
    <div class="tl-container">
      <div class="tl-nav">
        <button class="outline btn-compact" onclick="calPrev()">←</button>
        <span id="tl-range-label">${rangeLabel}</span>
        <button class="outline btn-compact" onclick="calNext()">→</button>
        <button class="outline btn-compact secondary" onclick="calToday()">Today</button>
      </div>
      <div class="tl-grid" id="tl-grid" style="grid-template-columns: 52px repeat(${weekdayDates.length}, 1fr)">
        <div class="tl-time-col">
          <div class="tl-corner"></div>
          ${timeLabels}
        </div>
        ${dayCols.join('')}
      </div>
    </div>`;

  window.calPrev = async () => {
    calStartDate = addDays(calStartDate, -7);
    await renderCalendarTab(document.getElementById('ana-content'));
  };
  window.calNext = async () => {
    calStartDate = addDays(calStartDate, 7);
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
  const weekdays = [1,2,3,4,5]; // Mon-Fri only
  const DOW_STR = { Sunday:0, Monday:1, Tuesday:2, Wednesday:3, Thursday:4, Friday:5, Saturday:6 };
  const dowInt = (r) => typeof r.dayOfWeek === 'number' ? r.dayOfWeek : (DOW_STR[r.dayOfWeek] ?? -1);

  // Filter out weekends
  const filteredWork = (toWork || []).filter(r => { const d = dowInt(r); return d >= 1 && d <= 5; })
    .sort((a,b) => dowInt(a) - dowInt(b));
  const filteredHome = (toHome || []).filter(r => { const d = dowInt(r); return d >= 1 && d <= 5; })
    .sort((a,b) => dowInt(a) - dowInt(b));

  function fmtHours(h) {
    if (!h) return '0m';
    const totalMin = Math.round(h * 60);
    const hr = Math.floor(totalMin / 60), mn = totalMin % 60;
    return hr > 0 ? `${hr}h ${mn}m` : `${mn}m`;
  }

  function commuteTable(data) {
    if (!data || !data.length) return '<p class="muted-sm">No data yet.</p>';
    return `<table class="stats-table">
      <thead><tr><th>Day</th><th>Avg</th><th>Best departure</th><th>Shortest</th><th>Trips</th></tr></thead>
      <tbody>
        ${data.map(r => `<tr>
          <td>${dowNames[dowInt(r)]}</td>
          <td>${fmtHours(r.averageDurationHours)}</td>
          <td>${String(Math.floor(r.optimalStartHour)).padStart(2,'0')}:00</td>
          <td>${fmtHours(r.shortestDurationHours)}</td>
          <td>${r.sessionCount}</td>
        </tr>`).join('')}
      </tbody>
    </table>`;
  }

  // Build chart data aligned to Mon-Fri
  const chartLabels = weekdays.map(d => dowNames[d]);
  const workMap = Object.fromEntries(filteredWork.map(r => [dowInt(r), r.averageDurationHours || 0]));
  const homeMap = Object.fromEntries(filteredHome.map(r => [dowInt(r), r.averageDurationHours || 0]));
  const workChartData = weekdays.map(d => workMap[d] || 0);
  const homeChartData = weekdays.map(d => homeMap[d] || 0);

  el.innerHTML = `
    <article>
      <strong>Commute duration by weekday</strong>
      <canvas id="commuteChart" style="max-height:220px;margin-top:0.75rem"></canvas>
    </article>
    <article>
      <strong>To Work</strong>
      ${commuteTable(filteredWork)}
    </article>
    <article>
      <strong>To Home</strong>
      ${commuteTable(filteredHome)}
    </article>`;

  // Render chart after DOM is ready
  const canvas = document.getElementById('commuteChart');
  if (canvas) {
    if (canvas._chartInstance) canvas._chartInstance.destroy();
    canvas._chartInstance = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: chartLabels,
        datasets: [
          { label: 'To Work', data: workChartData, backgroundColor: 'rgba(34,197,94,0.8)' },
          { label: 'To Home', data: homeChartData, backgroundColor: 'rgba(59,130,246,0.8)' }
        ]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { position: 'top' },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const h = ctx.parsed.y;
                if (!h) return `${ctx.dataset.label}: 0m`;
                const totalMin = Math.round(h * 60);
                const hr = Math.floor(totalMin / 60), mn = totalMin % 60;
                return `${ctx.dataset.label}: ${hr > 0 ? hr+'h ' : ''}${mn}m`;
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: { callback: v => v === 0 ? '0' : `${Math.floor(v)}h` }
          }
        }
      }
    });
  }
}

async function renderPatternsTab(el) {
  if (!_breakdown || !_breakdown.length) {
    el.innerHTML = '<p>No data yet. Try selecting a longer period.</p>';
    return;
  }

  const dowNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  const groups = {};
  for (let d = 1; d <= 5; d++) groups[d] = { work: [], commute: [], commuteHome: [], lunch: [] };

  for (const d of _breakdown) {
    const dow = new Date(d.date + 'T12:00:00Z').getUTCDay();
    if (dow < 1 || dow > 5) continue;
    if (d.workHours > 0) {
      groups[dow].work.push(d.workHours || 0);
      groups[dow].commute.push(d.commuteToWorkHours || 0);
      groups[dow].commuteHome.push(d.commuteToHomeHours || 0);
      groups[dow].lunch.push(d.lunchHours || 0);
    }
  }

  const avg = arr => arr.length ? arr.reduce((a,b) => a+b, 0) / arr.length : 0;

  const rows = [1,2,3,4,5].map(dow => {
    const g = groups[dow];
    if (!g.work.length) return '';
    return `<tr>
      <td>${dowNames[dow]}</td>
      <td>${fmtDur(avg(g.work))}</td>
      <td>${fmtDur(avg(g.commute))}</td>
      <td>${fmtDur(avg(g.commuteHome))}</td>
      <td>${fmtDur(avg(g.lunch))}</td>
      <td>${g.work.length}</td>
    </tr>`;
  }).join('');

  el.innerHTML = `
    <article>
      <strong>Average work week (Mon–Fri, days with work only)</strong>
      <table class="stats-table" style="margin-top:0.75rem">
        <thead>
          <tr><th>Day</th><th>Avg Work</th><th>Commute →Work</th><th>Commute →Home</th><th>Avg Lunch</th><th>Days</th></tr>
        </thead>
        <tbody>${rows || '<tr><td colspan="6">No data</td></tr>'}</tbody>
      </table>
    </article>`;
}

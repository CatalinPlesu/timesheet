import { fetchEntriesForRange, deleteEntry, updateEntry } from '../api.js';
import { fmtLocalDateTime, localDateISO } from '../time.js';

let entPeriodType = 'Day';
let entPeriodOffset = 0;
let entSortNewest = true;
let editingId = null;
let entTypeFilter = 'All';

function fmtDur(h) {
  if (!h) return '0m';
  const m = Math.round(h * 60);
  const hr = Math.floor(m / 60), mn = m % 60;
  if (hr > 0 && mn > 0) return `${hr}h ${mn}m`;
  if (hr > 0) return `${hr}h`;
  return `${mn}m`;
}

function getPeriodRange(type, offset) {
  const now = new Date();
  let start, end, label;

  if (type === 'Day') {
    start = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + offset));
    end = new Date(start);
    end.setUTCDate(start.getUTCDate() + 1);
    label = start.toISOString().slice(0, 10) + ' (' + ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'][start.getUTCDay()] + ')';
  } else if (type === 'Week') {
    const day = now.getUTCDay();
    const monday = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - (day === 0 ? 6 : day - 1) + offset * 7));
    const sunday = new Date(monday);
    sunday.setUTCDate(monday.getUTCDate() + 7);
    start = monday;
    end = sunday;
    label = `${monday.toISOString().slice(0, 10)} – ${new Date(sunday - 1).toISOString().slice(0, 10)}`;
  } else if (type === 'Month') {
    const y = now.getUTCFullYear(), m = now.getUTCMonth() + offset;
    start = new Date(Date.UTC(y, m, 1));
    end = new Date(Date.UTC(y, m + 1, 1));
    label = start.toLocaleString('en', { month: 'long', year: 'numeric', timeZone: 'UTC' });
  } else if (type === 'Year') {
    const y = now.getUTCFullYear() + offset;
    start = new Date(Date.UTC(y, 0, 1));
    end = new Date(Date.UTC(y + 1, 0, 1));
    label = String(y);
  } else {
    // All
    start = new Date(Date.UTC(2020, 0, 1));
    end = new Date(Date.UTC(now.getUTCFullYear() + 1, 0, 1));
    label = 'All time';
  }

  return {
    start: start.toISOString().slice(0, 10),
    end: end.toISOString().slice(0, 10),
    label
  };
}

function fmtDayLabel(isoDate) {
  const d = new Date(isoDate + 'T12:00:00Z');
  const days = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
  const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
  return `${days[d.getUTCDay()]}, ${d.getUTCDate()} ${months[d.getUTCMonth()]} ${d.getUTCFullYear()}`;
}

function entryRowClass(state) {
  if (!state) return '';
  const s = state.toLowerCase();
  if (s === 'working') return 'entry-work';
  if (s === 'commuting') return 'entry-commute';
  if (s === 'lunch') return 'entry-lunch';
  return '';
}

function stateBadge(state) {
  if (!state) return state;
  const s = state.toLowerCase();
  if (s === 'working')   return `<span class="badge badge-work">Working</span>`;
  if (s === 'commuting') return `<span class="badge badge-commute">Commuting</span>`;
  if (s === 'lunch')     return `<span class="badge badge-lunch">Lunch</span>`;
  return `<span class="badge">${state}</span>`;
}

export async function renderEntries() {
  document.getElementById('app').innerHTML = `
    <main class="container">
      <h2>Entries</h2>
      <p id="ent-success" class="success" style="display:none"></p>
      <p id="ent-error" class="error" style="display:none"></p>
      <div class="groupby-bar" id="period-type-bar"></div>
      <div class="groupby-bar" id="type-filter-bar"></div>
      <div id="sort-bar"></div>
      <div class="period-nav" id="period-nav"></div>
      <div id="entries-table"><p aria-busy="true">Loading…</p></div>
    </main>
  `;
  entPeriodOffset = 0;
  entTypeFilter = 'All';
  renderPeriodTypeBar();
  renderTypeFilterBar();
  renderSortBar();
  await loadPeriodEntries();
}

function renderPeriodTypeBar() {
  const bar = document.getElementById('period-type-bar');
  if (!bar) return;
  bar.innerHTML = ['Day','Week','Month','Year','All'].map(t =>
    `<button class="${t === entPeriodType ? 'secondary' : 'outline secondary'}" onclick="setPeriodType('${t}')">${t}</button>`
  ).join('');
  window.setPeriodType = async (t) => {
    entPeriodType = t;
    entPeriodOffset = 0;
    renderPeriodTypeBar();
    renderTypeFilterBar();
    renderSortBar();
    await loadPeriodEntries();
  };
}

function renderTypeFilterBar() {
  const bar = document.getElementById('type-filter-bar');
  if (!bar) return;
  const types = ['All', 'Working', 'Commuting', 'Lunch'];
  bar.innerHTML = types.map(t =>
    `<button class="${t === entTypeFilter ? 'secondary' : 'outline secondary'} btn-compact" onclick="setTypeFilter('${t}')">${t}</button>`
  ).join('');
  window.setTypeFilter = async (t) => {
    entTypeFilter = t;
    renderTypeFilterBar();
    await loadPeriodEntries();
  };
}

function renderSortBar() {
  const bar = document.getElementById('sort-bar');
  if (!bar) return;
  bar.innerHTML = `
    <div class="sort-bar">
      <small>Sort:</small>
      <button class="outline btn-compact ${entSortNewest ? 'secondary' : ''}" onclick="setSortNewest(true)">Newest first</button>
      <button class="outline btn-compact ${!entSortNewest ? 'secondary' : ''}" onclick="setSortNewest(false)">Oldest first</button>
    </div>`;
  window.setSortNewest = async (v) => { entSortNewest = v; renderSortBar(); await loadPeriodEntries(); };
}

function renderPeriodNav(label) {
  const el = document.getElementById('period-nav');
  if (!el) return;
  const hideNav = entPeriodType === 'All';
  el.innerHTML = hideNav
    ? `<p class="muted-sm">${label}</p>`
    : `
    <div style="display:flex; gap:0.5rem; align-items:center; margin-bottom:0.75rem; flex-wrap:wrap;">
      <button class="outline btn-compact" onclick="entPrevPeriod()">← Prev</button>
      <strong style="flex:1; text-align:center">${label}</strong>
      <button class="outline btn-compact" ${entPeriodOffset >= 0 ? 'disabled' : ''} onclick="entNextPeriod()">Next →</button>
    </div>`;
  window.entPrevPeriod = async () => {
    entPeriodOffset--;
    await loadPeriodEntries();
  };
  window.entNextPeriod = async () => {
    if (entPeriodOffset < 0) {
      entPeriodOffset++;
      await loadPeriodEntries();
    }
  };
}

async function loadPeriodEntries() {
  const { start, end, label } = getPeriodRange(entPeriodType, entPeriodOffset);
  renderPeriodNav(label);
  const tableEl = document.getElementById('entries-table');
  if (!tableEl) return;
  tableEl.innerHTML = '<p aria-busy="true">Loading…</p>';
  try {
    const data = await fetchEntriesForRange(start, end);
    if (!data) return;

    // Sort by startedAt
    const entries = (data.entries || []).sort((a, b) => {
      const diff = new Date(a.startedAt) - new Date(b.startedAt);
      return entSortNewest ? -diff : diff;
    });

    // Apply type filter
    const filteredEntries = entTypeFilter === 'All'
      ? entries
      : entries.filter(e => e.state && e.state.toLowerCase() === entTypeFilter.toLowerCase());

    if (filteredEntries.length === 0) {
      tableEl.innerHTML = '<p>No entries for this period.</p>';
      return;
    }

    // Group entries by local date
    const groups = new Map();
    for (const e of filteredEntries) {
      const dayKey = localDateISO(e.startedAt);
      if (!groups.has(dayKey)) groups.set(dayKey, []);
      groups.get(dayKey).push(e);
    }

    // Build rows with day separators
    let rows = '';
    for (const [dayKey, dayEntries] of groups) {
      rows += `<tr class="entry-day-sep"><td colspan="5"><span>${fmtDayLabel(dayKey)}</span></td></tr>`;
      rows += dayEntries.map(e => {
        const rowCls = entryRowClass(e.state);
        const noteIcon = e.note ? `<span class="note-icon" title="${e.note.replace(/"/g,'&quot;').replace(/\n/g,'&#10;')}">ℹ</span>` : '';
        const actionBtns = `<button class="outline btn-compact" onclick="toggleEdit('${e.id}')">✎</button>` +
          (!e.isActive ? `<button class="outline secondary btn-compact" onclick="delEntry('${e.id}')">✕</button>` : '');
        const editStartStr = fmtLocalDateTime(e.startedAt);
        const editEndStr = e.endedAt ? fmtLocalDateTime(e.endedAt) : 'active';
        const endRow = !e.isActive ? `<div style="display:flex;gap:0.5rem;align-items:center;flex-wrap:wrap;margin-top:0.3rem">
              <small style="min-width:4rem">End:</small>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','end',-30)">-30m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','end',-5)">-5m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','end',-1)">-1m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','end',1)">+1m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','end',5)">+5m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','end',30)">+30m</button>
            </div>` : '';
        const editRow = `<tr id="edit-row-${e.id}" style="display:none">
          <td colspan="5" style="background:var(--pico-card-background-color);padding:0.4rem 0.8rem">
            <div style="display:flex;gap:0.5rem;align-items:center;flex-wrap:wrap">
              <small style="color:var(--pico-muted-color)">${editStartStr} – ${editEndStr}</small>
            </div>
            <div style="display:flex;gap:0.5rem;align-items:center;flex-wrap:wrap;margin-top:0.3rem">
              <small style="min-width:4rem">Start:</small>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','start',-30)">-30m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','start',-5)">-5m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','start',-1)">-1m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','start',1)">+1m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','start',5)">+5m</button>
              <button class="outline btn-compact" onclick="applyAdjust('${e.id}','start',30)">+30m</button>
            </div>
            ${endRow}
          </td>
        </tr>`;
        return `<tr class="${rowCls}">
          <td>${stateBadge(e.state)}</td>
          <td>${fmtLocalDateTime(e.startedAt)}</td>
          <td>${e.endedAt ? fmtLocalDateTime(e.endedAt) : '—'}</td>
          <td>${e.durationHours != null ? fmtDur(e.durationHours) : '—'}</td>
          <td>${noteIcon}${actionBtns}</td>
        </tr>${editRow}`;
      }).join('');
    }

    tableEl.innerHTML = `
      <figure>
        <table role="grid">
          <thead><tr><th>Type</th><th>Started</th><th>Ended</th><th>Duration</th><th></th></tr></thead>
          <tbody class="entries-tbody">${rows}</tbody>
        </table>
      </figure>`;

    window.delEntry = async (id) => {
      if (!confirm('Delete this entry? This cannot be undone.')) return;
      try {
        await deleteEntry(id);
        const s = document.getElementById('ent-success');
        if (s) { s.textContent = 'Entry deleted.'; s.style.display = ''; }
        await loadPeriodEntries();
      } catch {
        const e = document.getElementById('ent-error');
        if (e) { e.textContent = 'Delete failed.'; e.style.display = ''; }
      }
    };

    window.toggleEdit = (id) => {
      if (editingId === id) {
        const row = document.getElementById(`edit-row-${id}`);
        if (row) row.style.display = 'none';
        editingId = null;
      } else {
        if (editingId) {
          const prev = document.getElementById(`edit-row-${editingId}`);
          if (prev) prev.style.display = 'none';
        }
        const row = document.getElementById(`edit-row-${id}`);
        if (row) row.style.display = '';
        editingId = id;
      }
    };

    window.applyAdjust = async (id, which, minutes) => {
      try {
        const opts = which === 'start'
          ? { startMinutes: minutes }
          : { endMinutes: minutes };
        await updateEntry(id, opts);
        await loadPeriodEntries();
        window.toggleEdit(id); // keep panel open
      } catch {
        const e = document.getElementById('ent-error');
        if (e) { e.textContent = 'Adjustment failed.'; e.style.display = ''; }
      }
    };
  } catch {
    tableEl.innerHTML = '<p class="error">Failed to load entries.</p>';
  }
}

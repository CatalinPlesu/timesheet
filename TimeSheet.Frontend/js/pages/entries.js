import { fetchEntries, deleteEntry } from '../api.js';

let entPage = 1;
let entGroupBy = 'Day';
let totalPages = 1;
let sortAsc = false;  // false = newest first (desc)

function fmtDateTime(s) {
  if (!s) return '—';
  return s.slice(0, 16).replace('T', ' ');
}

function fmtDur(h) {
  const m = Math.round(h * 60);
  const hr = Math.floor(m / 60), mn = m % 60;
  return hr > 0 ? `${hr}h ${mn}m` : `${mn}m`;
}

function dayLabel(iso) {
  const d = new Date(iso);
  const days = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  return `${iso.slice(0,10)} (${days[d.getUTCDay()]})`;
}

export async function renderEntries() {
  document.getElementById('app').innerHTML = `
    <main class="container">
      <h2>Entries</h2>
      <p id="ent-success" class="success" style="display:none"></p>
      <p id="ent-error" class="error" style="display:none"></p>
      <div class="groupby-bar" id="groupby-bar"></div>
      <div id="entries-table"><p aria-busy="true">Loading…</p></div>
      <div class="pagination" id="pagination"></div>
    </main>
  `;
  renderGroupBy();
  await loadEntries();
}

function renderGroupBy() {
  const bar = document.getElementById('groupby-bar');
  if (!bar) return;
  bar.innerHTML = ['Day','Week','Month','Year'].map(g =>
    `<button class="${g === entGroupBy ? 'secondary' : 'outline secondary'}" onclick="setGroupBy('${g}')">${g}</button>`
  ).join('') + `
    <button class="outline btn-compact" onclick="toggleSort()">
      Sort: ${sortAsc ? '↑ Oldest first' : '↓ Newest first'}
    </button>`;
  window.setGroupBy = async (g) => { entGroupBy = g; entPage = 1; await loadEntries(); renderGroupBy(); };
  window.toggleSort = async () => { sortAsc = !sortAsc; await loadEntries(); renderGroupBy(); };
}

async function loadEntries() {
  const tableEl = document.getElementById('entries-table');
  if (!tableEl) return;
  tableEl.innerHTML = '<p aria-busy="true">Loading…</p>';
  try {
    const data = await fetchEntries(entPage, 25, entGroupBy);
    if (!data) return;
    totalPages = data.totalPages || 1;
    const entries = data.entries || [];

    // Client-side sort by startedAt
    entries.sort((a, b) => {
      const d = new Date(a.startedAt) - new Date(b.startedAt);
      return sortAsc ? d : -d;
    });

    if (entries.length === 0) {
      tableEl.innerHTML = '<p>No entries.</p>';
    } else {
      let rows = '';
      let lastDate = null;

      if (entGroupBy === 'Day') {
        for (const e of entries) {
          const dateStr = e.startedAt ? e.startedAt.slice(0, 10) : null;
          if (dateStr && dateStr !== lastDate) {
            lastDate = dateStr;
            rows += `<tr class="group-header"><td colspan="5"><strong>${dayLabel(e.startedAt)}</strong></td></tr>`;
          }
          rows += `
            <tr>
              <td>${e.state}</td>
              <td>${fmtDateTime(e.startedAt)}</td>
              <td>${e.endedAt ? fmtDateTime(e.endedAt) : '—'}</td>
              <td>${e.durationHours != null ? fmtDur(e.durationHours) : '—'}</td>
              <td>${!e.isActive ? `<button class="outline secondary btn-compact" onclick="delEntry('${e.id}')">✕</button>` : ''}</td>
            </tr>`;
        }
      } else {
        for (const e of entries) {
          rows += `
            <tr>
              <td>${e.state}</td>
              <td>${fmtDateTime(e.startedAt)}</td>
              <td>${e.endedAt ? fmtDateTime(e.endedAt) : '—'}</td>
              <td>${e.durationHours != null ? fmtDur(e.durationHours) : '—'}</td>
              <td>${!e.isActive ? `<button class="outline secondary btn-compact" onclick="delEntry('${e.id}')">✕</button>` : ''}</td>
            </tr>`;
        }
      }

      tableEl.innerHTML = `
        <figure>
          <table role="grid">
            <thead><tr><th>Type</th><th>Started</th><th>Ended</th><th>Duration</th><th></th></tr></thead>
            <tbody>${rows}</tbody>
          </table>
        </figure>
        ${entGroupBy !== 'Day' ? '<p class="muted-sm">Note: week/month/year grouping shows entries in that period — headers not shown for multi-day groups.</p>' : ''}`;
    }
    renderPagination();
    window.delEntry = async (id) => {
      try {
        await deleteEntry(id);
        const s = document.getElementById('ent-success');
        s.textContent = 'Entry deleted.'; s.style.display = '';
        await loadEntries();
      } catch {
        const e = document.getElementById('ent-error');
        e.textContent = 'Delete failed.'; e.style.display = '';
      }
    };
  } catch(e) {
    tableEl.innerHTML = '<p class="error">Failed to load entries.</p>';
  }
}

function renderPagination() {
  const el = document.getElementById('pagination');
  if (!el) return;
  el.innerHTML = `
    ${entPage > 1 ? `<button class="outline" onclick="entPrev()">← Prev</button>` : ''}
    <p>Page ${entPage} / ${totalPages}</p>
    ${entPage < totalPages ? `<button class="outline" onclick="entNext()">Next →</button>` : ''}
  `;
  window.entPrev = async () => { entPage--; await loadEntries(); };
  window.entNext = async () => { entPage++; await loadEntries(); };
}

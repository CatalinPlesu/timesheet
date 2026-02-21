import { fetchEntries, deleteEntry } from '../api.js';

let entPage = 1;
let entGroupBy = 'Day';
let totalPages = 1;

function fmtDateTime(s) {
  if (!s) return '—';
  return s.slice(0, 16).replace('T', ' ');
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
  ).join('');
  window.setGroupBy = async (g) => { entGroupBy = g; entPage = 1; await loadEntries(); renderGroupBy(); };
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
    if (entries.length === 0) {
      tableEl.innerHTML = '<p>No entries.</p>';
    } else {
      tableEl.innerHTML = `
        <figure>
          <table role="grid">
            <thead><tr><th>Type</th><th>Started</th><th>Ended</th><th>Duration</th><th></th></tr></thead>
            <tbody>
              ${entries.map(e => `
                <tr>
                  <td>${e.state}</td>
                  <td>${fmtDateTime(e.startedAt)}</td>
                  <td>${e.endedAt ? fmtDateTime(e.endedAt) : '—'}</td>
                  <td>${e.durationHours != null ? fmtDur(e.durationHours) : '—'}</td>
                  <td>${!e.isActive ? `<button class="outline secondary btn-compact" onclick="delEntry('${e.id}')">✕</button>` : ''}</td>
                </tr>`).join('')}
            </tbody>
          </table>
        </figure>`;
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

function fmtDur(h) {
  const m = Math.round(h * 60);
  const hr = Math.floor(m / 60), mn = m % 60;
  return hr > 0 ? `${hr}h ${mn}m` : `${mn}m`;
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

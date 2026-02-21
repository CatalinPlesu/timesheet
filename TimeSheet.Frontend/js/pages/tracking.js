import { fetchCurrentState, toggleState } from '../api.js';

function fmtDur(hours) {
  const totalMin = Math.round(hours * 60);
  const h = Math.floor(totalMin / 60);
  const m = totalMin % 60;
  return h > 0 ? `${h}h ${m}m` : `${m}m`;
}

let state = { trkState: 'Idle', trkDurHours: 0 };

export async function renderTracking() {
  document.getElementById('app').innerHTML = `
    <main class="container">
      <h2>Tracking</h2>
      <p id="trk-success" class="success" style="display:none"></p>
      <p id="trk-error" class="error" style="display:none"></p>
      <article>
        <header>Current state</header>
        <p id="trk-state" class="state-label" aria-busy="true">Loading…</p>
        <p id="trk-dur"></p>
      </article>
      <article>
        <header>Toggle</header>
        <div class="grid-3">
          <button id="btn-commute" onclick="toggleBtn('Commuting')">Commute</button>
          <button id="btn-work"    onclick="toggleBtn('Working')">Work</button>
          <button id="btn-lunch"   onclick="toggleBtn('Lunch')">Lunch</button>
        </div>
      </article>
    </main>
  `;
  // expose toggleBtn globally for inline onclick
  window.toggleBtn = async (s) => {
    document.getElementById('trk-error').style.display = 'none';
    document.getElementById('trk-success').style.display = 'none';
    try {
      const data = await toggleState(s);
      if (data?.message) {
        const el = document.getElementById('trk-success');
        el.textContent = data.message;
        el.style.display = '';
      }
      await refreshTracking();
    } catch(e) {
      const el = document.getElementById('trk-error');
      el.textContent = 'Request failed.';
      el.style.display = '';
    }
  };
  await refreshTracking();
}

async function refreshTracking() {
  try {
    const data = await fetchCurrentState();
    if (!data) return;
    state.trkState = data.state || 'Idle';
    state.trkDurHours = data.durationHours || 0;
    document.getElementById('trk-state').textContent = state.trkState;
    document.getElementById('trk-state').removeAttribute('aria-busy');
    const durEl = document.getElementById('trk-dur');
    if (state.trkState !== 'Idle' && state.trkDurHours > 0) {
      durEl.textContent = fmtDur(state.trkDurHours);
    } else {
      durEl.textContent = '';
    }
    // Highlight active button
    for (const [id, s] of [['btn-commute','Commuting'],['btn-work','Working'],['btn-lunch','Lunch']]) {
      const btn = document.getElementById(id);
      if (btn) btn.className = (state.trkState === s) ? 'btn-active' : '';
    }
  } catch(e) {
    const el = document.getElementById('trk-error');
    if (el) { el.textContent = 'Failed to load state.'; el.style.display = ''; }
  }
}

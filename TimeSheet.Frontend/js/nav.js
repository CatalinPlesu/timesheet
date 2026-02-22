import { clearToken } from './auth.js';

export function renderNav(activePage) {
  const el = document.getElementById('nav-placeholder');
  if (!el) return;
  el.innerHTML = `
    <nav class="container-fluid">
      <ul><li><img src="logo.png" alt="TimeSheet" style="height:32px;width:auto;vertical-align:middle;margin-right:0.5rem;"><strong>TimeSheet</strong></li></ul>
      <ul>
        <li><a href="tracking.html" ${activePage==='tracking'?'class="contrast"':''}>Tracking</a></li>
        <li><a href="entries.html"  ${activePage==='entries' ?'class="contrast"':''}>Entries</a></li>
        <li><a href="analytics.html" ${activePage==='analytics'?'class="contrast"':''}>Analytics</a></li>
        <li><a href="#" id="nav-logout">Logout</a></li>
      </ul>
    </nav>`;
  document.getElementById('nav-logout').addEventListener('click', (e) => {
    e.preventDefault();
    clearToken();
    location.href = 'login.html';
  });
}

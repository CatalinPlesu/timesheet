import { isLoggedIn, clearToken } from './auth.js';
import { navigate, onNavigate } from './router.js';
import { renderLogin } from './pages/login.js';
import { renderTracking } from './pages/tracking.js';
import { renderEntries } from './pages/entries.js';
import { renderAnalytics } from './pages/analytics.js';

function renderNav(page) {
  const el = document.getElementById('nav-placeholder');
  if (page === 'login') { el.innerHTML = ''; return; }
  el.innerHTML = `
    <nav class="container-fluid">
      <ul><li><strong>TimeSheet</strong></li></ul>
      <ul>
        <li><a href="#" class="${page==='tracking'?'contrast':''}" onclick="navTo('tracking')">Tracking</a></li>
        <li><a href="#" class="${page==='entries'?'contrast':''}"  onclick="navTo('entries')">Entries</a></li>
        <li><a href="#" class="${page==='analytics'?'contrast':''}" onclick="navTo('analytics')">Analytics</a></li>
        <li><a href="#" onclick="navTo('logout')">Logout</a></li>
      </ul>
    </nav>`;
}

window.navTo = (page) => navigate(page);

onNavigate(async (page) => {
  renderNav(page);
  if (page === 'logout') { clearToken(); navigate('login'); return; }
  if (page === 'login')     await renderLogin();
  if (page === 'tracking')  await renderTracking();
  if (page === 'entries')   await renderEntries();
  if (page === 'analytics') await renderAnalytics();
});

// Handle 401 from anywhere
window.addEventListener('ts:logout', () => navigate('login'));

// Boot
if (isLoggedIn()) navigate('tracking');
else              navigate('login');

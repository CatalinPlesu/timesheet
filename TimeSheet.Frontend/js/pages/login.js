import { login } from '../api.js';
import { saveToken } from '../auth.js';
import { navigate } from '../router.js';

export function renderLogin() {
  document.getElementById('app').innerHTML = `
    <main class="container">
      <div class="login-wrap">
        <article>
          <header><h2>TimeSheet</h2></header>
          <p>Enter the one-time mnemonic from Telegram to log in.</p>
          <label for="mnemonic">Mnemonic phrase</label>
          <input type="text" id="mnemonic" placeholder="word1 word2 word3 …" autocomplete="off" />
          <p id="login-error" class="error" style="display:none"></p>
          <button id="login-btn">Login</button>
        </article>
      </div>
    </main>
  `;
  document.getElementById('login-btn').addEventListener('click', doLogin);
  document.getElementById('mnemonic').addEventListener('keydown', e => {
    if (e.key === 'Enter') doLogin();
  });
}

async function doLogin() {
  const mnemonic = document.getElementById('mnemonic').value.trim();
  if (!mnemonic) return;
  const btn = document.getElementById('login-btn');
  const err = document.getElementById('login-error');
  btn.setAttribute('aria-busy', 'true');
  btn.disabled = true;
  err.style.display = 'none';
  try {
    const data = await login(mnemonic);
    saveToken(data.accessToken);
    navigate('tracking');
  } catch {
    err.textContent = 'Invalid mnemonic. Please try again.';
    err.style.display = '';
  } finally {
    btn.removeAttribute('aria-busy');
    btn.disabled = false;
  }
}

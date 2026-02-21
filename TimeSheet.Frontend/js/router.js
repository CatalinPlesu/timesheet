// Pages: 'login' | 'tracking' | 'entries' | 'analytics'
let currentPage = 'login';
const listeners = [];

export function onNavigate(fn) { listeners.push(fn); }

export function navigate(page) {
  currentPage = page;
  listeners.forEach(fn => fn(page));
}

export function getPage() { return currentPage; }

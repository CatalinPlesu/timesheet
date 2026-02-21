import { API_BASE } from './config.js';
import { getToken, clearToken } from './auth.js';

async function request(method, path, body) {
  const token = getToken();
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  const res = await fetch(API_BASE + path, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined
  });
  if (res.status === 401) {
    clearToken();
    window.dispatchEvent(new CustomEvent('ts:logout'));
    return null;
  }
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

export const api = {
  get: (path) => request('GET', path),
  post: (path, body) => request('POST', path, body),
  del: (path) => request('DELETE', path),
};

// Domain calls
export async function fetchCurrentState() {
  return api.get('/api/tracking/current');
}
export async function toggleState(state) {
  return api.post('/api/tracking/toggle', { state });
}
export async function deleteEntry(id) {
  return api.del(`/api/entries/${id}`);
}
export async function login(mnemonic) {
  const headers = { 'Content-Type': 'application/json' };
  const res = await fetch(API_BASE + '/api/auth/login', {
    method: 'POST',
    headers,
    body: JSON.stringify({ mnemonic })
  });
  if (!res.ok) throw new Error('Invalid mnemonic');
  return res.json();
}
export async function fetchStats(days) {
  return api.get(`/api/analytics/stats-summary?days=${days}`);
}
export async function fetchChartData(startDate, endDate) {
  return api.get(`/api/analytics/chart-data?startDate=${startDate}&endDate=${endDate}&groupBy=Day`);
}
export async function fetchBreakdown(startDate, endDate) {
  return api.get(`/api/analytics/daily-breakdown?startDate=${startDate}&endDate=${endDate}`);
}
export async function fetchCommutePatterns(direction) {
  return api.get(`/api/analytics/commute-patterns?direction=${direction}`);
}
export async function fetchPeriodAggregate(startDate, endDate) {
  return api.get(`/api/analytics/period-aggregate?startDate=${startDate}&endDate=${endDate}`);
}
export async function fetchEntriesForRange(startDate, endDate) {
  return api.get(`/api/entries?startDate=${startDate}&endDate=${endDate}&pageSize=500&page=1`);
}

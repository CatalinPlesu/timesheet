const KEY = 'ts_token';
export const getToken = () => localStorage.getItem(KEY) || '';
export const saveToken = (t) => localStorage.setItem(KEY, t);
export const clearToken = () => localStorage.removeItem(KEY);
export const isLoggedIn = () => !!getToken();

const OFFSET_KEY = 'ts_utc_offset';
export const saveUtcOffset = (m) => localStorage.setItem(OFFSET_KEY, String(m));
export const getUtcOffset  = () => parseInt(localStorage.getItem(OFFSET_KEY) || '0', 10);

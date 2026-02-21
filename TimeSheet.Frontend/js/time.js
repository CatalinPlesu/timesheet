// time.js — uses browser's native timezone (no manual offset needed)

export function toLocal(utcStr) {
  if (!utcStr) return null;
  return new Date(utcStr); // Browser automatically applies local timezone
}

export function fmtLocalDateTime(utcStr) {
  if (!utcStr) return '—';
  const d = new Date(utcStr);
  const y = d.getFullYear();
  const mo = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  const h = String(d.getHours()).padStart(2, '0');
  const m = String(d.getMinutes()).padStart(2, '0');
  return `${y}-${mo}-${day} ${h}:${m}`;
}

export function fmtLocalTime(utcStr) {
  if (!utcStr) return '—';
  const d = new Date(utcStr);
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

export function localDateISO(utcStr) {
  if (!utcStr) return '';
  const d = new Date(utcStr);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function todayLocalISO() {
  return localDateISO(new Date().toISOString());
}

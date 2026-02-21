import { getUtcOffset } from './auth.js';

export function toLocal(utcStr) {
  if (!utcStr) return null;
  const d = new Date(utcStr);
  return new Date(d.getTime() + getUtcOffset() * 60000);
}

export function fmtLocalDateTime(utcStr) {
  if (!utcStr) return '—';
  const d = toLocal(utcStr);
  return d.toISOString().slice(0, 16).replace('T', ' ');
}

export function fmtLocalTime(utcStr) {
  if (!utcStr) return '—';
  const d = toLocal(utcStr);
  return d.toISOString().slice(11, 16);
}

export function localDateISO(utcStr) {
  if (!utcStr) return '';
  return toLocal(utcStr).toISOString().slice(0, 10);
}

export function todayLocalISO() {
  return toLocal(new Date().toISOString()).toISOString().slice(0, 10);
}

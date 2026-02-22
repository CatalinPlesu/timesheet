// API_BASE is empty so all fetch() calls use relative paths (e.g. /api/...).
// In production Caddy proxies /api/* to the api container internally.
// For local dev, run the API on its own port and set API_BASE there if needed.
export const API_BASE = '';

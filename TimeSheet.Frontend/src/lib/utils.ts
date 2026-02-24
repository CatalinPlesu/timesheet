import { type ClassValue, clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** Format a duration in hours as "Xh Ym" — never decimal hours */
export function fmtDur(hours: number | null | undefined): string {
  if (hours == null || isNaN(hours)) return '0m'
  const totalMin = Math.round(Math.abs(hours) * 60)
  const h = Math.floor(totalMin / 60)
  const m = totalMin % 60
  if (h > 0 && m > 0) return `${h}h ${m}m`
  if (h > 0) return `${h}h`
  return `${m}m`
}

/** Format a UTC date string as local datetime "YYYY-MM-DD HH:MM" */
export function fmtLocalDateTime(utcStr: string | null | undefined): string {
  if (!utcStr) return '—'
  const d = new Date(utcStr)
  const y = d.getFullYear()
  const mo = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const h = String(d.getHours()).padStart(2, '0')
  const mi = String(d.getMinutes()).padStart(2, '0')
  return `${y}-${mo}-${day} ${h}:${mi}`
}

/** Format UTC string as local time "HH:MM" */
export function fmtLocalTime(utcStr: string | null | undefined): string {
  if (!utcStr) return '—'
  const d = new Date(utcStr)
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

/** Get local ISO date from UTC string "YYYY-MM-DD" */
export function localDateISO(utcStr: string | null | undefined): string {
  if (!utcStr) return ''
  const d = new Date(utcStr)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Today's local date as ISO string */
export function todayLocalISO(): string {
  return localDateISO(new Date().toISOString())
}

/** Parse UTC time string (Timily — stored without Z, treat as UTC) */
export function parseEmployerTime(utcStr: string | null | undefined): Date | null {
  if (!utcStr) return null
  const s = utcStr.endsWith('Z') ? utcStr : utcStr + 'Z'
  return new Date(s)
}

/** Format employer UTC time as local HH:MM */
export function fmtClockTime(utcStr: string | null | undefined): string {
  const d = parseEmployerTime(utcStr)
  if (!d) return '—'
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

const DOW_NAMES = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
const MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export { DOW_NAMES, MONTH_NAMES }

/** Format an ISO date string as "Day DD Mon YYYY" */
export function fmtDayLabel(isoDate: string): string {
  const d = new Date(isoDate + 'T12:00:00Z')
  return `${['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'][d.getUTCDay()]}, ${d.getUTCDate()} ${MONTH_NAMES[d.getUTCMonth()]} ${d.getUTCFullYear()}`
}

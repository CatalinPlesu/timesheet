// Typed API client for TimeSheet
// Base URL from env var (default empty = same origin, proxied by nginx/Caddy)
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

const TOKEN_KEY = 'ts_token'
const UTC_OFFSET_KEY = 'ts_utc_offset'

export const auth = {
  getToken: (): string => localStorage.getItem(TOKEN_KEY) ?? '',
  saveToken: (t: string): void => { localStorage.setItem(TOKEN_KEY, t) },
  clearToken: (): void => { localStorage.removeItem(TOKEN_KEY) },
  isLoggedIn: (): boolean => !!localStorage.getItem(TOKEN_KEY),
  saveUtcOffset: (m: number): void => { localStorage.setItem(UTC_OFFSET_KEY, String(m)) },
  getUtcOffset: (): number => parseInt(localStorage.getItem(UTC_OFFSET_KEY) ?? '0', 10),
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T | null> {
  const token = auth.getToken()
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(API_BASE + path, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (res.status === 401) {
    auth.clearToken()
    window.dispatchEvent(new CustomEvent('ts:logout'))
    return null
  }

  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  const text = await res.text()
  return text ? (JSON.parse(text) as T) : null
}

// ─── Auth ────────────────────────────────────────────────────────────────────

export interface LoginResponse {
  accessToken: string
  utcOffsetMinutes: number
}

export async function apiLogin(mnemonic: string): Promise<LoginResponse> {
  const res = await fetch(API_BASE + '/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ mnemonic }),
  })
  if (!res.ok) throw new Error('Invalid mnemonic')
  return res.json() as Promise<LoginResponse>
}

// ─── Tracking ────────────────────────────────────────────────────────────────

export type TrackingState = 'Idle' | 'Working' | 'Commuting' | 'Lunch'

export interface CurrentStateResponse {
  state: TrackingState
  startedAt?: string | null
  durationHours?: number | null
  message?: string
  commuteDirection?: string | null
}

export interface ToggleResponse {
  newState: TrackingState
  previousState?: TrackingState | null
  message: string
  startedAt?: string | null
  previousSessionDurationHours?: number | null
}

export async function fetchCurrentState(): Promise<CurrentStateResponse | null> {
  return request<CurrentStateResponse>('GET', '/api/tracking/current')
}

export async function toggleState(state: TrackingState, offsetMinutes?: number): Promise<ToggleResponse | null> {
  if (offsetMinutes !== undefined && offsetMinutes !== 0) {
    // API: positive OffsetMinutes = started in the past
    // UI: negative preset value = started in the past (e.g. -30 = 30 min ago)
    // So we negate the UI value to get the API value
    return request<ToggleResponse>('POST', '/api/tracking/toggle-with-offset', {
      state,
      offsetMinutes: -offsetMinutes,
    })
  }
  return request<ToggleResponse>('POST', '/api/tracking/toggle', { state })
}

// ─── Entries ──────────────────────────────────────────────────────────────────

export interface Entry {
  id: string
  state: TrackingState
  startedAt: string
  endedAt: string | null
  durationHours: number | null
  isActive: boolean
  note: string | null
}

export interface EntriesResponse {
  entries: Entry[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export async function fetchEntriesForRange(startDate: string, endDate: string): Promise<EntriesResponse | null> {
  return request<EntriesResponse>('GET', `/api/entries?startDate=${startDate}&endDate=${endDate}&pageSize=500&page=1`)
}

export async function deleteEntry(id: string): Promise<null> {
  return request<null>('DELETE', `/api/entries/${id}`)
}

export async function updateEntry(id: string, opts: { startMinutes?: number; endMinutes?: number }): Promise<null> {
  const body: { startAdjustmentMinutes?: number; endAdjustmentMinutes?: number } = {}
  if (opts.startMinutes !== undefined) body.startAdjustmentMinutes = opts.startMinutes
  if (opts.endMinutes !== undefined) body.endAdjustmentMinutes = opts.endMinutes
  return request<null>('PUT', `/api/entries/${id}`, body)
}

// ─── Analytics ───────────────────────────────────────────────────────────────

export interface StatMetric {
  avg: number
  min: number
  max: number
  stdDev: number
  total: number
}

export interface StatsSummaryResponse {
  periodDays: number
  daysWithData: number
  work: StatMetric
  commuteToWork: StatMetric
  commuteToHome: StatMetric
  lunch: StatMetric
}

export async function fetchStats(days: number): Promise<StatsSummaryResponse | null> {
  return request<StatsSummaryResponse>('GET', `/api/analytics/stats-summary?days=${days}`)
}

export interface ChartDataResponse {
  labels: string[]
  workHours: number[]
  commuteHours: number[]
  lunchHours: number[]
  idleHours: number[]
}

export async function fetchChartData(startDate: string, endDate: string): Promise<ChartDataResponse | null> {
  return request<ChartDataResponse>('GET', `/api/analytics/chart-data?startDate=${startDate}&endDate=${endDate}&groupBy=Day`)
}

export interface DailyBreakdown {
  date: string
  workHours: number
  commuteToWorkHours: number
  commuteToHomeHours: number
  lunchHours: number
  officeSpanHours: number | null
}

export async function fetchBreakdown(startDate: string, endDate: string): Promise<DailyBreakdown[]> {
  const result = await request<DailyBreakdown[]>('GET', `/api/analytics/daily-breakdown?startDate=${startDate}&endDate=${endDate}`)
  return result ?? []
}

export interface CommutePattern {
  dayOfWeek: number | string
  averageDurationHours: number
  shortestDurationHours: number | null
  optimalStartHour: number | null
  sessionCount: number
}

export async function fetchCommutePatterns(direction: 'ToWork' | 'ToHome'): Promise<CommutePattern[]> {
  const result = await request<CommutePattern[]>('GET', `/api/analytics/commute-patterns?direction=${direction}`)
  return result ?? []
}

export interface PeriodAggregate {
  totalWorkHours: number
  totalCommuteHours: number
  totalLunchHours: number
  workDaysCount: number
}

export async function fetchPeriodAggregate(startDate: string, endDate: string): Promise<PeriodAggregate | null> {
  return request<PeriodAggregate>('GET', `/api/analytics/period-aggregate?startDate=${startDate}&endDate=${endDate}`)
}

// ─── Compliance ───────────────────────────────────────────────────────────────

export interface Violation {
  date: string
  ruleType: string
  actualHours: number | null
  thresholdHours: number
  description: string
}

export interface ViolationsResponse {
  violations: Violation[]
  violationCount: number
  totalDays: number
}

export async function fetchViolations(from: string, to: string): Promise<ViolationsResponse> {
  const data = await request<ViolationsResponse>('GET', `/api/compliance/violations?from=${from}&to=${to}`)
  return data ?? { violations: [], violationCount: 0, totalDays: 0 }
}

// ─── Employer Attendance ──────────────────────────────────────────────────────

export interface EmployerRecord {
  date: string
  clockIn: string | null
  clockOut: string | null
  workingHours: number | null
  hasConflict: boolean
  conflictType: string | null
  eventTypes: string
}

export interface EmployerAttendanceResponse {
  records: EmployerRecord[]
  lastImport: string | null
  totalRecords: number
}

export async function fetchEmployerAttendance(from: string, to: string): Promise<EmployerAttendanceResponse> {
  const data = await request<EmployerAttendanceResponse>('GET', `/api/employer-attendance?from=${from}&to=${to}`)
  return data ?? { records: [], lastImport: null, totalRecords: 0 }
}

// ─── Settings ─────────────────────────────────────────────────────────────────

export interface UserSettings {
  utcOffsetMinutes: number
  targetWorkHours?: number
  targetOfficeHours?: number
}

export async function fetchSettings(): Promise<UserSettings | null> {
  return request<UserSettings>('GET', '/api/settings')
}

// ─── Date range helpers ───────────────────────────────────────────────────────

export function dateRange(days: number): { startDate: string; endDate: string } {
  const end = new Date()
  const start = new Date(end.getTime() - days * 86400000)
  return {
    startDate: start.toISOString().slice(0, 10),
    endDate: end.toISOString().slice(0, 10),
  }
}

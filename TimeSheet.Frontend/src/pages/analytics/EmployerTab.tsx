import { useState } from 'react'
import { type EmployerAttendanceResponse, type UserSettings, type EmployerRecord } from '@/lib/api'
import { fmtDur, parseEmployerTime } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

// ─── Helpers ─────────────────────────────────────────────────────────────────

const MONTH_NAMES = [
  'January','February','March','April','May','June',
  'July','August','September','October','November','December',
]

function isWeekendISO(dateStr: string): boolean {
  const d = new Date(dateStr + 'T12:00:00Z')
  const dow = d.getUTCDay()
  return dow === 0 || dow === 6
}

function isHoliday(r: EmployerRecord): boolean {
  return (r.eventTypes ?? '').toLowerCase().includes('holiday')
}

function isAbsence(r: EmployerRecord): boolean {
  return (r.eventTypes ?? '').toLowerCase().includes('absence')
}

/** Build a month calendar grid: array of 6 weeks × 7 days */
function buildMonthGrid(year: number, month: number): (string | null)[] {
  // month is 0-indexed
  const firstDay = new Date(Date.UTC(year, month, 1))
  const lastDay = new Date(Date.UTC(year, month + 1, 0))
  // Shift to Mon-first (0=Mon … 6=Sun)
  const startDow = (firstDay.getUTCDay() + 6) % 7
  const totalDays = lastDay.getUTCDate()
  const cells: (string | null)[] = []
  for (let i = 0; i < startDow; i++) cells.push(null)
  for (let d = 1; d <= totalDays; d++) {
    cells.push(`${year}-${String(month + 1).padStart(2,'0')}-${String(d).padStart(2,'0')}`)
  }
  // Pad to full 6-week grid (42 cells)
  while (cells.length < 42) cells.push(null)
  return cells
}

/** Format employer clock time respecting utcOffsetMinutes */
function fmtClockWithOffset(utcStr: string | null | undefined, utcOffsetMinutes: number): string {
  const d = parseEmployerTime(utcStr)
  if (!d) return '—'
  const localMs = d.getTime() + utcOffsetMinutes * 60000
  const local = new Date(localMs)
  return `${String(local.getUTCHours()).padStart(2,'0')}:${String(local.getUTCMinutes()).padStart(2,'0')}`
}

// ─── Day cell ─────────────────────────────────────────────────────────────────

interface DayCellProps {
  dateStr: string
  record: EmployerRecord | undefined
  targetH: number | null
  utcOffsetMinutes: number
  today: string
}

function DayCell({ dateStr, record, targetH, utcOffsetMinutes, today }: DayCellProps) {
  const isToday = dateStr === today
  const weekend = isWeekendISO(dateStr)
  const holiday = record ? isHoliday(record) : false
  const absent = record ? isAbsence(record) : false
  const hasConflict = record?.hasConflict && !absent
  const hasData = record && record.workingHours != null && !absent

  // Background class
  let bgClass = ''
  if (hasConflict) {
    bgClass = 'bg-red-500/10 border-l-2 border-red-500'
  } else if (weekend || holiday) {
    bgClass = 'bg-muted/30'
  } else if (hasData) {
    bgClass = 'bg-green-500/10'
  }

  const dayNum = parseInt(dateStr.slice(8), 10)

  const clockIn = hasData ? fmtClockWithOffset(record!.clockIn, utcOffsetMinutes) : null
  const clockOut = hasData ? fmtClockWithOffset(record!.clockOut, utcOffsetMinutes) : null
  const spanStr = hasData && record!.workingHours != null ? fmtDur(record!.workingHours) : null

  return (
    <div
      className={cn(
        'min-h-[90px] p-1.5 rounded-md border border-border/30 flex flex-col gap-0.5',
        bgClass,
        isToday && 'ring-1 ring-primary'
      )}
    >
      {/* Date number */}
      <div className={cn(
        'text-xs font-medium self-end',
        isToday ? 'text-primary font-bold' : 'text-muted-foreground'
      )}>
        {dayNum}
      </div>

      {/* Content */}
      {hasData && clockIn && clockOut && (
        <p className="text-xs font-mono leading-tight text-foreground/80 truncate">
          {clockIn} → {clockOut}
        </p>
      )}
      {spanStr && (
        <p className="text-xs text-foreground/70 leading-tight">{spanStr}</p>
      )}
      {targetH != null && hasData && record!.workingHours != null && (
        (() => {
          const diff = record!.workingHours - targetH
          const abs = fmtDur(Math.abs(diff))
          return (
            <p className={cn('text-xs font-medium leading-tight', diff >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400')}>
              {diff >= 0 ? `+${abs}` : `−${abs}`}
            </p>
          )
        })()
      )}
      {hasConflict && (
        <Badge variant="destructive" className="text-[10px] px-1 py-0 h-4 self-start mt-auto">
          Conflict
        </Badge>
      )}
      {absent && (
        <span className="text-[10px] text-muted-foreground mt-auto">Absence</span>
      )}
      {holiday && !absent && (
        <span className="text-[10px] text-muted-foreground mt-auto">Holiday</span>
      )}
    </div>
  )
}

// ─── Main component ────────────────────────────────────────────────────────────

interface Props {
  employer: EmployerAttendanceResponse | null
  settings: UserSettings | null
}

export function EmployerTab({ employer, settings }: Props) {
  const data = employer ?? { records: [], lastImport: null, totalRecords: 0 }
  const records = data.records ?? []
  const utcOffsetMinutes = settings?.utcOffsetMinutes ?? 0

  const targetH = settings?.targetOfficeHours
    ? Number(settings.targetOfficeHours)
    : settings?.targetWorkHours ? Number(settings.targetWorkHours) : null

  // Default calendar month: most recent record or today
  const todayDate = new Date()
  const defaultYear = todayDate.getFullYear()
  const defaultMonth = todayDate.getMonth() // 0-indexed

  const [calYear, setCalYear] = useState(defaultYear)
  const [calMonth, setCalMonth] = useState(defaultMonth)

  const today = `${todayDate.getFullYear()}-${String(todayDate.getMonth()+1).padStart(2,'0')}-${String(todayDate.getDate()).padStart(2,'0')}`

  // Build a lookup map for fast access
  const recordMap = new Map<string, EmployerRecord>()
  for (const r of records) recordMap.set(r.date, r)

  // Last import
  let lastImportStr = ''
  if (data.lastImport) {
    const d = new Date(data.lastImport)
    const MONTHS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']
    lastImportStr = `${MONTHS[d.getMonth()]} ${String(d.getDate()).padStart(2,'0')}, ${d.getFullYear()}`
  }

  // Reserve calculation (all available records)
  let reserveEl: React.ReactNode = null
  if (targetH != null) {
    const daysWithData = records.filter(r => r.workingHours != null && !isAbsence(r))
    const totalWorked = daysWithData.reduce((s, r) => s + (r.workingHours ?? 0), 0)
    const totalTarget = daysWithData.length * targetH
    const reserveHours = totalWorked - totalTarget
    const absDur = fmtDur(Math.abs(reserveHours))
    reserveEl = (
      <p className="text-sm mt-2">
        Reserve ({daysWithData.length} days × {fmtDur(targetH)} target):{' '}
        {reserveHours >= 0 ? (
          <span className="text-green-600 dark:text-green-400 font-medium">+{absDur} ahead</span>
        ) : (
          <span className="text-red-600 dark:text-red-400 font-medium">−{absDur} behind</span>
        )}
      </p>
    )
  }

  // Month grid
  const cells = buildMonthGrid(calYear, calMonth)

  const handlePrevMonth = () => {
    if (calMonth === 0) { setCalYear(y => y - 1); setCalMonth(11) }
    else setCalMonth(m => m - 1)
  }
  const handleNextMonth = () => {
    if (calMonth === 11) { setCalYear(y => y + 1); setCalMonth(0) }
    else setCalMonth(m => m + 1)
  }

  const DOW_HEADERS = ['Mon','Tue','Wed','Thu','Fri','Sat','Sun']

  // Months in selected month for conflict count
  const monthPrefix = `${calYear}-${String(calMonth + 1).padStart(2,'0')}-`
  const monthRecords = records.filter(r => r.date.startsWith(monthPrefix))
  const monthConflicts = monthRecords.filter(r => r.hasConflict && !isAbsence(r)).length

  return (
    <div className="space-y-4">
      {/* Header card */}
      <div className="rounded-lg border border-border/50 overflow-hidden">
        <div className="px-4 py-3 bg-muted/20 border-b border-border/30">
          <p className="text-sm font-semibold">Employer Attendance</p>
        </div>
        <div className="px-4 py-3">
          {lastImportStr ? (
            <p className="text-sm text-muted-foreground">
              Last imported: <strong className="text-foreground">{lastImportStr}</strong>
            </p>
          ) : (
            <p className="text-sm text-muted-foreground">
              No data imported yet. Use <code className="bg-muted px-1 py-0.5 rounded text-xs">/import &lt;token&gt;</code> in Telegram.
            </p>
          )}
          {reserveEl}
        </div>
      </div>

      {/* Calendar section */}
      <div className="rounded-lg border border-border/50 overflow-hidden">
        {/* Month navigation */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-border/30 bg-muted/10">
          <Button variant="outline" size="icon" className="h-7 w-7" onClick={handlePrevMonth}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <div className="text-center">
            <span className="text-sm font-semibold">{MONTH_NAMES[calMonth]} {calYear}</span>
            {monthConflicts > 0 && (
              <span className="ml-2 text-xs text-red-600 dark:text-red-400">
                {monthConflicts} conflict{monthConflicts > 1 ? 's' : ''}
              </span>
            )}
          </div>
          <Button variant="outline" size="icon" className="h-7 w-7" onClick={handleNextMonth}>
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>

        {/* Day-of-week headers */}
        <div className="grid grid-cols-7 border-b border-border/30 bg-muted/20">
          {DOW_HEADERS.map(d => (
            <div key={d} className={cn(
              'py-1.5 text-center text-xs font-medium text-muted-foreground',
              (d === 'Sat' || d === 'Sun') && 'text-muted-foreground/50'
            )}>
              {d}
            </div>
          ))}
        </div>

        {/* Calendar grid */}
        <div className="grid grid-cols-7 gap-1 p-2">
          {cells.map((dateStr, idx) => {
            if (!dateStr) {
              return <div key={`empty-${idx}`} className="min-h-[90px]" />
            }
            return (
              <DayCell
                key={dateStr}
                dateStr={dateStr}
                record={recordMap.get(dateStr)}
                targetH={targetH}
                utcOffsetMinutes={utcOffsetMinutes}
                today={today}
              />
            )
          })}
        </div>

        {/* Legend */}
        <div className="flex flex-wrap gap-3 px-4 py-2 border-t border-border/30 text-xs text-muted-foreground bg-muted/10">
          <span className="flex items-center gap-1.5">
            <span className="w-3 h-3 rounded bg-green-500/30 border border-green-500/40 inline-block" />
            Good day
          </span>
          <span className="flex items-center gap-1.5">
            <span className="w-3 h-3 rounded bg-red-500/10 border-l-2 border-red-500 inline-block" />
            Conflict
          </span>
          <span className="flex items-center gap-1.5">
            <span className="w-3 h-3 rounded bg-muted/50 border border-border/30 inline-block" />
            Weekend / no data
          </span>
        </div>
      </div>
    </div>
  )
}

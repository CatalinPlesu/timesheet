import { useState, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchEntriesForRange, type EmployerAttendanceResponse } from '@/lib/api'
import { localDateISO, fmtDur } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { ChevronLeft, ChevronRight, Loader2 } from 'lucide-react'

const PX_PER_MIN = 1.5
const DEFAULT_HOUR_MIN = 6
const DEFAULT_HOUR_MAX = 20

function isWeekend(d: Date) { const day = d.getUTCDay(); return day === 0 || day === 6 }

function getMondayOfWeek(d: Date): Date {
  const r = new Date(d)
  const dow = r.getUTCDay()
  const diff = dow === 0 ? -6 : 1 - dow
  r.setUTCDate(r.getUTCDate() + diff)
  return r
}

function addDays(d: Date, n: number): Date {
  const r = new Date(d)
  r.setUTCDate(r.getUTCDate() + n)
  return r
}

function isoDate(d: Date): string { return d.toISOString().slice(0, 10) }

function sessionColorClass(state: string): string {
  const s = state.toLowerCase()
  if (s === 'working') return 'bg-blue-500/80 border-blue-400/40'
  if (s === 'commuting') return 'bg-green-500/80 border-green-400/40'
  if (s === 'lunch') return 'bg-orange-500/80 border-orange-400/40'
  return 'bg-blue-500/80 border-blue-400/40'
}

function fmtTime(d: Date): string {
  return `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`
}

function visibleDays(): number {
  if (typeof window === 'undefined') return 5
  if (window.innerWidth < 640) return 1
  if (window.innerWidth < 1024) return 3
  return 5
}

const DOW_SHORT = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat']
const MONTH_SHORT = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']

interface Props {
  employer: EmployerAttendanceResponse | null
}

export function CalendarTab({ employer }: Props) {
  const [calStart, setCalStart] = useState<Date>(() => getMondayOfWeek(new Date()))
  const n = visibleDays()

  // Build weekday dates (skip weekends)
  const weekdayDates: Date[] = []
  let cur = new Date(calStart)
  while (weekdayDates.length < n) {
    if (!isWeekend(cur)) weekdayDates.push(new Date(cur))
    cur.setUTCDate(cur.getUTCDate() + 1)
  }

  const rangeStart = isoDate(calStart)
  const rangeEnd = isoDate(addDays(weekdayDates[weekdayDates.length - 1], 1))

  const { data, isLoading } = useQuery({
    queryKey: ['calEntries', rangeStart, rangeEnd],
    queryFn: () => fetchEntriesForRange(rangeStart, rangeEnd),
  })

  const entries = data?.entries ?? []

  // Determine hour range
  let hourMin = DEFAULT_HOUR_MIN
  let hourMax = DEFAULT_HOUR_MAX
  for (const e of entries) {
    const s = new Date(e.startedAt)
    const h1 = s.getHours() + s.getMinutes() / 60
    hourMin = Math.min(hourMin, Math.floor(h1) - 1)
    if (e.endedAt) {
      const en = new Date(e.endedAt)
      const h2 = en.getHours() + en.getMinutes() / 60
      hourMax = Math.max(hourMax, Math.ceil(h2) + 1)
    }
  }
  hourMin = Math.max(0, Math.min(hourMin, DEFAULT_HOUR_MIN))
  hourMax = Math.min(24, Math.max(hourMax, DEFAULT_HOUR_MAX))
  const totalHours = hourMax - hourMin
  const totalPx = totalHours * PX_PER_MIN * 60

  const hourLabels: string[] = []
  for (let h = hourMin; h < hourMax; h++) {
    hourLabels.push(`${String(h).padStart(2,'0')}:00`)
  }

  const handlePrev = useCallback(() => setCalStart(d => addDays(d, -7)), [])
  const handleNext = useCallback(() => setCalStart(d => addDays(d, 7)), [])
  const handleToday = useCallback(() => setCalStart(getMondayOfWeek(new Date())), [])

  const lastDate = weekdayDates[weekdayDates.length - 1]
  const rangeLabel = isoDate(calStart) === isoDate(lastDate)
    ? `${DOW_SHORT[calStart.getUTCDay()]} ${MONTH_SHORT[calStart.getUTCMonth()]} ${calStart.getUTCDate()}, ${calStart.getUTCFullYear()}`
    : `${DOW_SHORT[calStart.getUTCDay()]} ${MONTH_SHORT[calStart.getUTCMonth()]} ${calStart.getUTCDate()} – ${DOW_SHORT[lastDate.getUTCDay()]} ${MONTH_SHORT[lastDate.getUTCMonth()]} ${lastDate.getUTCDate()}, ${lastDate.getUTCFullYear()}`

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-16 text-muted-foreground">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {/* Navigation */}
      <div className="flex items-center gap-2">
        <Button variant="outline" size="icon" className="h-7 w-7" onClick={handlePrev}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <span className="text-sm font-medium flex-1 text-center">{rangeLabel}</span>
        <Button variant="outline" size="icon" className="h-7 w-7" onClick={handleNext}>
          <ChevronRight className="h-4 w-4" />
        </Button>
        <Button variant="outline" size="sm" className="h-7 text-xs" onClick={handleToday}>Today</Button>
      </div>

      {/* Timeline grid */}
      <div className="overflow-x-auto rounded-lg border border-border/50">
        <div
          className="grid min-w-[300px]"
          style={{ gridTemplateColumns: `48px repeat(${weekdayDates.length}, 1fr)` }}
        >
          {/* Time column */}
          <div className="border-r border-border/30">
            {/* Header spacer */}
            <div className="h-10 border-b border-border/30" />
            {hourLabels.map(h => (
              <div
                key={h}
                className="text-xs text-muted-foreground/60 text-right pr-1.5 -mt-2 pointer-events-none select-none"
                style={{ height: PX_PER_MIN * 60 }}
              >
                {h}
              </div>
            ))}
          </div>

          {/* Day columns */}
          {weekdayDates.map((colDate, colIdx) => {
            const colDateStr = isoDate(colDate)
            const dayEntries = entries.filter(e => e.startedAt && localDateISO(e.startedAt) === colDateStr)

            const localMidnightMs = new Date(colDateStr + 'T00:00:00').getTime()
            const dayStartMs = localMidnightMs + hourMin * 3600000

            const empRecord = employer?.records?.find(r => r.date === colDateStr)

            return (
              <div key={colDateStr} className={cn('border-l border-border/30', colIdx > 0 && 'border-l')}>
                {/* Header */}
                <div className="h-10 border-b border-border/30 flex flex-col items-center justify-center bg-muted/20">
                  <span className="text-xs font-medium">{DOW_SHORT[colDate.getUTCDay()]}</span>
                  <span className="text-xs text-muted-foreground">
                    {MONTH_SHORT[colDate.getUTCMonth()]} {colDate.getUTCDate()}
                  </span>
                </div>

                {/* Body */}
                <div className="relative" style={{ height: totalPx }}>
                  {/* Hour grid lines */}
                  {hourLabels.map((_, idx) => (
                    <div
                      key={idx}
                      className="absolute left-0 right-0 border-t border-border/20"
                      style={{ top: idx * PX_PER_MIN * 60 }}
                    />
                  ))}

                  {/* Session blocks */}
                  {dayEntries.map(entry => {
                    const sessionStart = new Date(entry.startedAt)
                    const sessionEnd = entry.endedAt ? new Date(entry.endedAt) : new Date()
                    const topPx = Math.max(0, (sessionStart.getTime() - dayStartMs) / 60000 * PX_PER_MIN)
                    const heightPx = Math.max((sessionEnd.getTime() - sessionStart.getTime()) / 60000 * PX_PER_MIN, PX_PER_MIN * 10)
                    const dur = entry.durationHours != null ? fmtDur(entry.durationHours) : fmtDur((sessionEnd.getTime() - sessionStart.getTime()) / 3600000)
                    const titleText = `${entry.state}\n${fmtTime(sessionStart)} – ${entry.endedAt ? fmtTime(sessionEnd) : 'now'} (${dur})${entry.note ? '\n' + entry.note : ''}`

                    return (
                      <div
                        key={entry.id}
                        className={cn(
                          'absolute left-1 right-1 rounded border px-1 overflow-hidden text-white text-xs leading-tight cursor-default',
                          sessionColorClass(entry.state),
                          entry.isActive && 'ring-1 ring-white/50'
                        )}
                        style={{ top: topPx, height: heightPx }}
                        title={titleText}
                      >
                        {heightPx >= 24 && (
                          <span className="block truncate">{entry.state}</span>
                        )}
                        {heightPx >= 40 && (
                          <span className="block truncate opacity-80">{dur}</span>
                        )}
                      </div>
                    )
                  })}

                  {/* Employer overlay bar */}
                  {empRecord?.clockIn && empRecord?.clockOut && (() => {
                    const toUtc = (s: string) => new Date(s.endsWith('Z') ? s : s + 'Z')
                    const empIn = toUtc(empRecord.clockIn)
                    const empOut = toUtc(empRecord.clockOut)
                    const topPx = Math.max(0, (empIn.getTime() - dayStartMs) / 60000 * PX_PER_MIN)
                    const botPx = Math.max(0, (empOut.getTime() - dayStartMs) / 60000 * PX_PER_MIN)
                    const heightPx = Math.max(botPx - topPx, PX_PER_MIN * 5)
                    const empInTime = `${String(empIn.getHours()).padStart(2,'0')}:${String(empIn.getMinutes()).padStart(2,'0')}`
                    const empOutTime = `${String(empOut.getHours()).padStart(2,'0')}:${String(empOut.getMinutes()).padStart(2,'0')}`
                    const totalMins = Math.round((empOut.getTime() - empIn.getTime()) / 60000)
                    const empDur = fmtDur(totalMins / 60)
                    return (
                      <div
                        className="absolute left-0 w-1.5 bg-red-500/70 rounded-r cursor-help"
                        style={{ top: topPx, height: heightPx }}
                        title={`Employer: ${empInTime} – ${empOutTime} (${empDur})`}
                      />
                    )
                  })()}
                </div>
              </div>
            )
          })}
        </div>
      </div>

      {/* Legend */}
      <div className="flex flex-wrap gap-3 text-xs text-muted-foreground">
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-blue-500/80 inline-block" /> Work</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-green-500/80 inline-block" /> Commute</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-orange-500/80 inline-block" /> Lunch</span>
        <span className="flex items-center gap-1.5"><span className="w-1.5 h-3 rounded bg-red-500/70 inline-block" /> Employer</span>
      </div>
    </div>
  )
}

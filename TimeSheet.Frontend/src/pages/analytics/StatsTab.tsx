import { type StatsSummaryResponse, type DailyBreakdown, type PeriodAggregate, type ViolationsResponse, type StatMetric } from '@/lib/api'
import { fmtDur, DOW_NAMES } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { CheckCircle2, AlertTriangle } from 'lucide-react'

function median(arr: number[]): number {
  if (!arr.length) return 0
  const s = [...arr].sort((a, b) => a - b)
  const m = Math.floor(s.length / 2)
  return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2
}

interface StatsRowProps {
  label: string
  obj: StatMetric | null
  med?: number
}

function StatsRow({ label, obj, med }: StatsRowProps) {
  if (!obj) {
    return (
      <tr className="border-b border-border/30">
        <td className="px-3 py-2 text-sm">{label}</td>
        {Array.from({ length: 6 }).map((_, i) => (
          <td key={i} className="px-3 py-2 text-sm text-muted-foreground">—</td>
        ))}
      </tr>
    )
  }
  return (
    <tr className="border-b border-border/30 hover:bg-muted/20 transition-colors">
      <td className="px-3 py-2 text-sm font-medium">{label}</td>
      <td className="px-3 py-2 text-sm tabular-nums">{fmtDur(obj.avg)}</td>
      <td className="px-3 py-2 text-sm tabular-nums text-muted-foreground">{med !== undefined ? fmtDur(med) : '—'}</td>
      <td className="px-3 py-2 text-sm tabular-nums text-muted-foreground">{fmtDur(obj.min)}</td>
      <td className="px-3 py-2 text-sm tabular-nums text-muted-foreground">{fmtDur(obj.max)}</td>
      <td className="px-3 py-2 text-sm tabular-nums text-muted-foreground">{fmtDur(obj.stdDev)}</td>
      <td className="px-3 py-2 text-sm tabular-nums">{fmtDur(obj.total)}</td>
    </tr>
  )
}

function fmtViolationDate(isoDate: string): string {
  const d = new Date(isoDate + 'T12:00:00Z')
  return `${DOW_NAMES[d.getUTCDay()]} ${String(d.getUTCDate()).padStart(2, '0')}/${String(d.getUTCMonth() + 1).padStart(2, '0')}`
}

interface Props {
  stats: StatsSummaryResponse | null
  breakdown: DailyBreakdown[]
  periodAggregate: PeriodAggregate | null
  violations: ViolationsResponse | null
  anaPeriod: number
}

export function StatsTab({ stats, breakdown, periodAggregate, violations, anaPeriod }: Props) {
  if (!stats) {
    return <p className="text-muted-foreground py-8 text-center">No stats yet. Start tracking to see data here.</p>
  }

  // Compute medians from breakdown
  const workValues = breakdown.map(d => d.workHours ?? 0).filter(h => h > 0)
  const workMedian = median(workValues)
  const commuteWorkMedian = median(breakdown.map(d => d.commuteToWorkHours ?? 0).filter(h => h > 0))
  const commuteHomeMedian = median(breakdown.map(d => d.commuteToHomeHours ?? 0).filter(h => h > 0))
  const lunchMedian = median(breakdown.map(d => d.lunchHours ?? 0).filter(h => h > 0))

  // Idle stats
  const idleValues = breakdown
    .filter(d => (d.officeSpanHours ?? 0) > 0)
    .map(d => Math.max(0, (d.officeSpanHours ?? 0) - (d.workHours ?? 0) - (d.lunchHours ?? 0)))
  const idleMedian = median(idleValues)
  const idleAvg = idleValues.length ? idleValues.reduce((a, b) => a + b, 0) / idleValues.length : 0
  const idleMin = idleValues.length ? Math.min(...idleValues) : 0
  const idleMax = idleValues.length ? Math.max(...idleValues) : 0
  const idleTotal = idleValues.reduce((a, b) => a + b, 0)
  const idleStdDev = idleValues.length
    ? Math.sqrt(idleValues.reduce((a, b) => a + (b - idleAvg) ** 2, 0) / idleValues.length)
    : 0
  const idleStatObj: StatMetric | null = idleValues.length
    ? { avg: idleAvg, min: idleMin, max: idleMax, stdDev: idleStdDev, total: idleTotal }
    : null

  // Day-of-week breakdown
  const dowGroups: Record<number, { work: number[]; commute: number[]; lunch: number[] }> = {}
  for (let i = 0; i < 7; i++) dowGroups[i] = { work: [], commute: [], lunch: [] }
  for (const d of breakdown) {
    if (!d.workHours || d.workHours <= 0) continue
    const dow = new Date(d.date.slice(0, 10) + 'T12:00:00Z').getUTCDay()
    dowGroups[dow].work.push(d.workHours)
    dowGroups[dow].commute.push((d.commuteToWorkHours ?? 0) + (d.commuteToHomeHours ?? 0))
    dowGroups[dow].lunch.push(d.lunchHours ?? 0)
  }

  // Average office span
  const officeSpanValues = breakdown.map(d => d.officeSpanHours).filter((h): h is number => h != null)
  const avgOfficeSpan = officeSpanValues.length
    ? officeSpanValues.reduce((a, b) => a + b, 0) / officeSpanValues.length
    : null

  const periodLabel = anaPeriod === 3650
    ? `All time · ${stats.daysWithData} days with data`
    : `${stats.periodDays} days in period · ${stats.daysWithData} days with data`

  const avg = (arr: number[]) => arr.length ? arr.reduce((a, b) => a + b, 0) / arr.length : 0

  return (
    <div className="space-y-4">
      <p className="text-xs text-muted-foreground">{periodLabel}</p>

      {/* Main stats table */}
      <div className="rounded-lg border border-border/50 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border/50 bg-muted/30">
                {['Metric', 'Avg', 'Median', 'Min', 'Max', 'Std Dev', 'Total'].map(h => (
                  <th key={h} className="px-3 py-2.5 text-left font-medium text-muted-foreground text-xs uppercase tracking-wide">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              <StatsRow label="Work" obj={stats.work} med={workMedian} />
              <StatsRow label="Commute →Work" obj={stats.commuteToWork} med={commuteWorkMedian} />
              <StatsRow label="Commute →Home" obj={stats.commuteToHome} med={commuteHomeMedian} />
              <StatsRow label="Lunch" obj={stats.lunch} med={lunchMedian} />
              <StatsRow label="Idle (in office)" obj={idleStatObj} med={idleMedian} />
            </tbody>
          </table>
        </div>
      </div>

      {/* Avg office span card */}
      {avgOfficeSpan != null && (
        <div className="inline-flex flex-col p-4 rounded-lg border border-border/50 bg-card">
          <span className="text-xs text-muted-foreground">Avg office span</span>
          <span className="text-2xl font-bold mt-0.5">{fmtDur(avgOfficeSpan)}</span>
          <span className="text-xs text-muted-foreground mt-0.5">
            {officeSpanValues.length} day{officeSpanValues.length !== 1 ? 's' : ''} with data
          </span>
        </div>
      )}

      {/* Period totals */}
      {periodAggregate && (
        <div className="rounded-lg border border-border/50 p-4 bg-card">
          <p className="text-sm font-semibold mb-2">Period Totals</p>
          <div className="flex flex-wrap gap-4 text-sm">
            <span><span className="text-blue-500 font-medium">{fmtDur(periodAggregate.totalWorkHours)}</span> work</span>
            <span><span className="text-green-400 font-medium">{fmtDur(periodAggregate.totalCommuteToWorkHours)}</span> commute →work</span>
            <span><span className="text-green-600 font-medium">{fmtDur(periodAggregate.totalCommuteToHomeHours)}</span> commute →home</span>
            <span><span className="text-green-500 font-medium">{fmtDur(periodAggregate.totalCommuteHours)}</span> commute total</span>
            <span><span className="text-orange-500 font-medium">{fmtDur(periodAggregate.totalLunchHours)}</span> lunch</span>
            <span className="text-muted-foreground">{periodAggregate.workDaysCount ?? '—'} work days</span>
            {avgOfficeSpan != null && (
              <span className="text-muted-foreground">Avg span: {fmtDur(avgOfficeSpan)}</span>
            )}
          </div>
        </div>
      )}

      {/* Compliance */}
      {violations && (
        <div className="rounded-lg border border-border/50 overflow-hidden">
          <div className="px-4 py-3 bg-muted/20 border-b border-border/30">
            <p className="text-sm font-semibold">Compliance</p>
          </div>
          {violations.violationCount === 0 ? (
            <div className="px-4 py-3 flex items-center gap-2 text-green-600 dark:text-green-400">
              <CheckCircle2 className="h-4 w-4" />
              <span className="text-sm">All days compliant</span>
            </div>
          ) : (
            <div>
              <p className="px-4 py-2 text-xs text-muted-foreground">
                {violations.violationCount} violation{violations.violationCount !== 1 ? 's' : ''} in period
              </p>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border/30 bg-muted/10">
                      {['Date', 'Actual', 'Required', 'Status'].map(h => (
                        <th key={h} className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {violations.violations
                      .slice()
                      .sort((a, b) => b.date.localeCompare(a.date))
                      .map(v => {
                        const diff = (v.actualHours ?? 0) - v.thresholdHours
                        const isSlightly = diff >= -1
                        return (
                          <tr key={v.date} className="border-b border-border/20 hover:bg-muted/10">
                            <td className="px-3 py-2 text-sm font-medium">{fmtViolationDate(v.date)}</td>
                            <td className="px-3 py-2 text-sm tabular-nums">{fmtDur(v.actualHours)}</td>
                            <td className="px-3 py-2 text-sm tabular-nums text-muted-foreground">{fmtDur(v.thresholdHours)}</td>
                            <td className="px-3 py-2">
                              <span className={cn(
                                'inline-flex items-center gap-1 text-xs px-1.5 py-0.5 rounded font-medium',
                                isSlightly
                                  ? 'bg-yellow-500/10 text-yellow-600 dark:text-yellow-400 border border-yellow-500/30'
                                  : 'bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/30'
                              )}>
                                <AlertTriangle className="h-3 w-3" />
                                {fmtDur(Math.abs(diff))} short
                              </span>
                            </td>
                          </tr>
                        )
                      })}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Day of week breakdown */}
      {Object.values(dowGroups).some(g => g.work.length > 0) && (
        <div className="rounded-lg border border-border/50 overflow-hidden">
          <div className="px-4 py-3 bg-muted/20 border-b border-border/30">
            <p className="text-sm font-semibold">By Day of Week</p>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/30 bg-muted/10">
                  {['Day', 'Avg Work', 'Avg Commute', 'Avg Lunch', 'Trips'].map(h => (
                    <th key={h} className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {Object.entries(dowGroups).map(([dow, g]) => {
                  if (!g.work.length) return null
                  return (
                    <tr key={dow} className="border-b border-border/20 hover:bg-muted/10">
                      <td className="px-3 py-2 font-medium">{DOW_NAMES[Number(dow)]}</td>
                      <td className="px-3 py-2 tabular-nums text-blue-500">{fmtDur(avg(g.work))}</td>
                      <td className="px-3 py-2 tabular-nums text-green-500">{fmtDur(avg(g.commute))}</td>
                      <td className="px-3 py-2 tabular-nums text-orange-500">{fmtDur(avg(g.lunch))}</td>
                      <td className="px-3 py-2 tabular-nums text-muted-foreground">{g.work.length}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

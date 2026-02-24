import { useState, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip,
  Legend, ResponsiveContainer, ReferenceLine
} from 'recharts'
import { fetchChartData, type DailyBreakdown, type EmployerAttendanceResponse } from '@/lib/api'
import { fmtDur } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { ChevronLeft, ChevronRight, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'

// Custom tooltip
function CustomTooltip({ active, payload, label }: {
  active?: boolean; payload?: Array<{ name: string; value: number; color: string }>; label?: string
}) {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border border-border/50 bg-popover px-3 py-2 shadow-xl text-sm">
      <p className="font-medium mb-1">{label}</p>
      {payload.map(p => (
        p.value > 0 && (
          <p key={p.name} style={{ color: p.color }} className="text-xs">
            {p.name}: {fmtDur(p.value)}
          </p>
        )
      ))}
    </div>
  )
}

interface ChartDataPoint {
  label: string
  work: number
  commute: number
  lunch: number
  idle: number
  officeSpan: number | null
  employer: number | null
  isWeekend: boolean
}

interface Props {
  breakdown: DailyBreakdown[]
  employer: EmployerAttendanceResponse | null
}

export function ChartTab({ breakdown, employer }: Props) {
  const [chartWindowStart, setChartWindowStart] = useState(() => {
    const now = new Date()
    const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - 13))
    return d
  })
  const [chartType, setChartType] = useState<'bar' | 'line'>('bar')

  const windowEnd = new Date(chartWindowStart)
  windowEnd.setUTCDate(windowEnd.getUTCDate() + 14)

  const startStr = chartWindowStart.toISOString().slice(0, 10)
  const endStr = windowEnd.toISOString().slice(0, 10)

  const { data: chartData, isLoading } = useQuery({
    queryKey: ['chartData', startStr, endStr],
    queryFn: () => fetchChartData(startStr, endStr),
  })

  const handlePrev = useCallback(() => {
    setChartWindowStart(d => {
      const n = new Date(d)
      n.setUTCDate(n.getUTCDate() - 14)
      return n
    })
  }, [])

  const handleNext = useCallback(() => {
    setChartWindowStart(d => {
      const n = new Date(d)
      n.setUTCDate(n.getUTCDate() + 14)
      return n
    })
  }, [])

  // Build chart data
  const apiMap: Record<string, { work: number; commute: number; lunch: number; idle: number }> = {}
  if (chartData?.labels) {
    chartData.labels.forEach((lbl, i) => {
      apiMap[lbl] = {
        work: chartData.workHours?.[i] ?? 0,
        commute: chartData.commuteHours?.[i] ?? 0,
        lunch: chartData.lunchHours?.[i] ?? 0,
        idle: chartData.idleHours?.[i] ?? 0,
      }
    })
  }

  const officeSpanMap: Record<string, number> = {}
  for (const d of breakdown) {
    if (d.officeSpanHours != null) officeSpanMap[d.date.slice(0, 10)] = d.officeSpanHours
  }

  const employerHoursMap: Record<string, number> = {}
  for (const r of (employer?.records ?? [])) {
    if (r.workingHours != null) employerHoursMap[r.date] = r.workingHours
  }

  const dataPoints: ChartDataPoint[] = []
  const cur = new Date(chartWindowStart)
  while (cur < windowEnd) {
    const ds = cur.toISOString().slice(0, 10)
    const dow = cur.getUTCDay()
    const d = apiMap[ds] ?? { work: 0, commute: 0, lunch: 0, idle: 0 }
    dataPoints.push({
      label: ds.slice(5), // MM-DD
      work: d.work,
      commute: d.commute,
      lunch: d.lunch,
      idle: d.idle,
      officeSpan: officeSpanMap[ds] ?? null,
      employer: employerHoursMap[ds] ?? null,
      isWeekend: dow === 0 || dow === 6,
    })
    cur.setUTCDate(cur.getUTCDate() + 1)
  }

  const hasOfficeSpan = dataPoints.some(d => d.officeSpan != null)
  const hasEmployer = dataPoints.some(d => d.employer != null)

  const COLORS = {
    work: '#3b82f6',
    commute: '#22c55e',
    lunch: '#f97316',
    idle: 'hsl(var(--muted-foreground) / 0.5)',
    officeSpan: '#a855f7',
    employer: '#ef4444',
    target: '#ef444470',
  }

  const tickStyle = { fontSize: 11, fill: 'hsl(var(--muted-foreground))' }

  const formatTick = (v: number) => v === 0 ? '0' : `${Math.floor(v)}h`

  const windowLabel = `${startStr} – ${new Date(windowEnd.getTime() - 1).toISOString().slice(0, 10)}`

  return (
    <div className="space-y-4">
      {/* Controls */}
      <div className="flex items-center gap-2 flex-wrap">
        <Button variant="outline" size="icon" className="h-7 w-7" onClick={handlePrev}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <span className="text-sm font-medium flex-1 text-center">{windowLabel}</span>
        <Button variant="outline" size="icon" className="h-7 w-7" onClick={handleNext}>
          <ChevronRight className="h-4 w-4" />
        </Button>

        <div className="flex items-center gap-1 rounded-lg border border-border/50 p-1 bg-muted/30">
          {(['bar', 'line'] as const).map(t => (
            <button
              key={t}
              onClick={() => setChartType(t)}
              className={cn(
                'px-2.5 py-1 text-xs rounded-md font-medium capitalize transition-colors',
                chartType === t ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
              )}
            >
              {t}
            </button>
          ))}
        </div>
      </div>

      {/* Chart */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16 text-muted-foreground">
          <Loader2 className="h-6 w-6 animate-spin" />
        </div>
      ) : (
        <div className="w-full" style={{ height: 320 }}>
          <ResponsiveContainer width="100%" height="100%">
            {chartType === 'bar' ? (
              <BarChart data={dataPoints} margin={{ top: 5, right: 5, bottom: 5, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border) / 0.5)" />
                <XAxis dataKey="label" tick={tickStyle} tickFormatter={(v, i) => {
                  const dp = dataPoints[i]
                  return dp?.isWeekend ? '' : v
                }} />
                <YAxis tick={tickStyle} tickFormatter={formatTick} width={35} />
                <Tooltip content={<CustomTooltip />} />
                <Legend iconSize={10} iconType="circle" wrapperStyle={{ fontSize: 12 }} />
                <Bar dataKey="work" name="Work" fill={COLORS.work} radius={[2, 2, 0, 0]} maxBarSize={40} />
                <Bar dataKey="commute" name="Commute" fill={COLORS.commute} radius={[2, 2, 0, 0]} maxBarSize={40} />
                <Bar dataKey="lunch" name="Lunch" fill={COLORS.lunch} radius={[2, 2, 0, 0]} maxBarSize={40} />
                <Bar dataKey="idle" name="Idle" fill={COLORS.idle} radius={[2, 2, 0, 0]} maxBarSize={40} />
                {hasOfficeSpan && (
                  <Line
                    dataKey="officeSpan" name="Office span"
                    stroke={COLORS.officeSpan} strokeWidth={2}
                    dot={{ r: 3, fill: COLORS.officeSpan }} type="monotone"
                    connectNulls={false}
                  />
                )}
                {hasEmployer && (
                  <Line
                    dataKey="employer" name="Employer"
                    stroke={COLORS.employer} strokeWidth={2}
                    dot={{ r: 4, fill: COLORS.employer }} type="monotone"
                    connectNulls={false}
                  />
                )}
              </BarChart>
            ) : (
              <LineChart data={dataPoints} margin={{ top: 5, right: 5, bottom: 5, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border) / 0.5)" />
                <XAxis dataKey="label" tick={tickStyle} tickFormatter={(v, i) => {
                  const dp = dataPoints[i]
                  return dp?.isWeekend ? '' : v
                }} />
                <YAxis tick={tickStyle} tickFormatter={formatTick} width={35} />
                <Tooltip content={<CustomTooltip />} />
                <Legend iconSize={10} iconType="circle" wrapperStyle={{ fontSize: 12 }} />
                <ReferenceLine y={8} stroke={COLORS.target} strokeDasharray="8 4" strokeWidth={1.5} label={{ value: '8h', fill: COLORS.target, fontSize: 11 }} />
                <Line dataKey="work" name="Work" stroke={COLORS.work} strokeWidth={2} dot={false} type="monotone" fill={COLORS.work + '1a'} />
                <Line dataKey="commute" name="Commute" stroke={COLORS.commute} strokeWidth={2} dot={false} type="monotone" />
                <Line dataKey="lunch" name="Lunch" stroke={COLORS.lunch} strokeWidth={2} dot={false} type="monotone" />
                <Line dataKey="idle" name="Idle" stroke={COLORS.idle} strokeWidth={1.5} dot={false} strokeDasharray="5 5" type="monotone" />
                {hasOfficeSpan && (
                  <Line dataKey="officeSpan" name="Office span" stroke={COLORS.officeSpan} strokeWidth={2} dot={{ r: 3 }} type="monotone" connectNulls={false} />
                )}
                {hasEmployer && (
                  <Line dataKey="employer" name="Employer" stroke={COLORS.employer} strokeWidth={2} dot={{ r: 4 }} type="monotone" connectNulls={false} />
                )}
              </LineChart>
            )}
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}

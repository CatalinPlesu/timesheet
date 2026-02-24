import { useQuery } from '@tanstack/react-query'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import { fetchCommutePatterns, fetchSettings, type DailyBreakdown } from '@/lib/api'
import { fmtDur, DOW_NAMES } from '@/lib/utils'
import { Loader2 } from 'lucide-react'

const DOW_STR: Record<string, number> = {
  Sunday: 0, Monday: 1, Tuesday: 2, Wednesday: 3, Thursday: 4, Friday: 5, Saturday: 6
}

function dowInt(r: { dayOfWeek: number | string }): number {
  return typeof r.dayOfWeek === 'number' ? r.dayOfWeek : (DOW_STR[r.dayOfWeek as string] ?? -1)
}

function CustomTooltip({ active, payload, label }: {
  active?: boolean; payload?: Array<{ name: string; value: number; color: string }>; label?: string
}) {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border border-border/50 bg-popover px-3 py-2 shadow-xl text-sm">
      <p className="font-medium mb-1">{label}</p>
      {payload.map(p => (
        <p key={p.name} style={{ color: p.color }} className="text-xs">
          {p.name}: {fmtDur(p.value)}
        </p>
      ))}
    </div>
  )
}

interface Props {
  breakdown: DailyBreakdown[]
}

export function CommuteTab({ breakdown }: Props) {
  const { data: toWork, isLoading: loadingWork } = useQuery({
    queryKey: ['commutePatterns', 'ToWork'],
    queryFn: () => fetchCommutePatterns('ToWork'),
  })

  const { data: toHome, isLoading: loadingHome } = useQuery({
    queryKey: ['commutePatterns', 'ToHome'],
    queryFn: () => fetchCommutePatterns('ToHome'),
  })

  const { data: settings } = useQuery({
    queryKey: ['settings'],
    queryFn: fetchSettings,
    staleTime: 5 * 60 * 1000,
  })
  const utcOffsetMinutes = settings?.utcOffsetMinutes ?? 0

  const isLoading = loadingWork || loadingHome

  const weekdays = [1, 2, 3, 4, 5]
  const filteredWork = (toWork ?? []).filter(r => { const d = dowInt(r); return d >= 1 && d <= 5 }).sort((a, b) => dowInt(a) - dowInt(b))
  const filteredHome = (toHome ?? []).filter(r => { const d = dowInt(r); return d >= 1 && d <= 5 }).sort((a, b) => dowInt(a) - dowInt(b))

  const workMap = Object.fromEntries(filteredWork.map(r => [dowInt(r), r.averageDurationHours ?? 0]))
  const homeMap = Object.fromEntries(filteredHome.map(r => [dowInt(r), r.averageDurationHours ?? 0]))

  const chartData = weekdays.map(d => ({
    day: DOW_NAMES[d],
    toWork: workMap[d] ?? 0,
    toHome: homeMap[d] ?? 0,
  }))

  const breakdownWithSpan = breakdown
    .filter(d => d.officeSpanHours != null)
    .sort((a, b) => b.date < a.date ? -1 : 1)
    .slice(0, 30)

  function CommuteTable({ data }: { data: typeof filteredWork }) {
    if (!data.length) return <p className="text-sm text-muted-foreground">No data yet.</p>
    return (
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border/30 bg-muted/10">
              {['Day', 'Avg', 'Best departure', 'Shortest', 'Trips'].map(h => (
                <th key={h} className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map(r => {
              // optimalStartHour is UTC — convert to local by adding utcOffsetMinutes
              let bestDeparture = '—'
              if (r.optimalStartHour != null) {
                const localHour = ((Math.floor(r.optimalStartHour) + Math.round(utcOffsetMinutes / 60)) % 24 + 24) % 24
                bestDeparture = `${String(localHour).padStart(2,'0')}:00`
              }
              return (
                <tr key={dowInt(r)} className="border-b border-border/20 hover:bg-muted/10">
                  <td className="px-3 py-2 font-medium">{DOW_NAMES[dowInt(r)]}</td>
                  <td className="px-3 py-2 tabular-nums">{fmtDur(r.averageDurationHours)}</td>
                  <td className="px-3 py-2 tabular-nums font-medium">{bestDeparture}</td>
                  <td className="px-3 py-2 tabular-nums">{fmtDur(r.shortestDurationHours)}</td>
                  <td className="px-3 py-2 tabular-nums text-muted-foreground">{r.sessionCount}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-16 text-muted-foreground">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Chart */}
      <div className="rounded-lg border border-border/50 p-4">
        <p className="text-sm font-semibold mb-3">Commute duration by weekday</p>
        <div style={{ height: 220 }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border) / 0.5)" />
              <XAxis dataKey="day" tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }} />
              <YAxis tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }} tickFormatter={v => v === 0 ? '0' : `${Math.floor(v)}h`} width={35} />
              <Tooltip content={<CustomTooltip />} />
              <Legend iconSize={10} iconType="circle" wrapperStyle={{ fontSize: 12 }} />
              <Bar dataKey="toWork" name="To Work" fill="#22c55e" radius={[2, 2, 0, 0]} maxBarSize={40} />
              <Bar dataKey="toHome" name="To Home" fill="#3b82f6" radius={[2, 2, 0, 0]} maxBarSize={40} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* To work table */}
      <div className="rounded-lg border border-border/50 overflow-hidden">
        <div className="px-4 py-3 bg-muted/20 border-b border-border/30">
          <p className="text-sm font-semibold">To Work</p>
        </div>
        <div className="p-4">
          <CommuteTable data={filteredWork} />
        </div>
      </div>

      {/* To home table */}
      <div className="rounded-lg border border-border/50 overflow-hidden">
        <div className="px-4 py-3 bg-muted/20 border-b border-border/30">
          <p className="text-sm font-semibold">To Home</p>
        </div>
        <div className="p-4">
          <CommuteTable data={filteredHome} />
        </div>
      </div>

      {/* Office span per day */}
      <div className="rounded-lg border border-border/50 overflow-hidden">
        <div className="px-4 py-3 bg-muted/20 border-b border-border/30">
          <p className="text-sm font-semibold">Office span per day</p>
          <p className="text-xs text-muted-foreground mt-0.5">Time from arriving at office to leaving</p>
        </div>
        <div className="overflow-x-auto">
          {breakdownWithSpan.length === 0 ? (
            <p className="px-4 py-3 text-sm text-muted-foreground">No office span data yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/30 bg-muted/10">
                  {['Date', 'Office span', 'Commute →Work', 'Commute →Home'].map(h => (
                    <th key={h} className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {breakdownWithSpan.map(d => (
                  <tr key={d.date} className="border-b border-border/20 hover:bg-muted/10">
                    <td className="px-3 py-2 font-mono text-xs">{d.date}</td>
                    <td className="px-3 py-2 tabular-nums">{fmtDur(d.officeSpanHours)}</td>
                    <td className="px-3 py-2 tabular-nums text-green-500">{fmtDur(d.commuteToWorkHours)}</td>
                    <td className="px-3 py-2 tabular-nums text-blue-500">{fmtDur(d.commuteToHomeHours)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}

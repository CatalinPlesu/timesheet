import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import { type DailyBreakdown } from '@/lib/api'
import { fmtDur, DOW_NAMES } from '@/lib/utils'

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

interface Props {
  breakdown: DailyBreakdown[]
}

export function PatternsTab({ breakdown }: Props) {
  if (!breakdown.length) {
    return (
      <p className="text-muted-foreground py-8 text-center">
        No data yet. Try selecting a longer period.
      </p>
    )
  }

  const groups: Record<number, { work: number[]; commute: number[]; commuteHome: number[]; lunch: number[]; idle: number[] }> = {}
  for (let d = 1; d <= 5; d++) groups[d] = { work: [], commute: [], commuteHome: [], lunch: [], idle: [] }

  for (const d of breakdown) {
    const dow = new Date(d.date.slice(0, 10) + 'T12:00:00Z').getUTCDay()
    if (dow < 1 || dow > 5) continue
    if (d.workHours > 0) {
      groups[dow].work.push(d.workHours ?? 0)
      groups[dow].commute.push(d.commuteToWorkHours ?? 0)
      groups[dow].commuteHome.push(d.commuteToHomeHours ?? 0)
      groups[dow].lunch.push(d.lunchHours ?? 0)
      if ((d.officeSpanHours ?? 0) > 0) {
        groups[dow].idle.push(Math.max(0, (d.officeSpanHours ?? 0) - (d.workHours ?? 0) - (d.lunchHours ?? 0)))
      }
    }
  }

  const avg = (arr: number[]) => arr.length ? arr.reduce((a, b) => a + b, 0) / arr.length : 0

  const chartData = [1, 2, 3, 4, 5].map(d => ({
    day: DOW_NAMES[d],
    work: avg(groups[d].work),
    commute: avg([...groups[d].commute]),
    lunch: avg(groups[d].lunch),
    idle: avg(groups[d].idle),
  }))

  return (
    <div className="space-y-4">
      {/* Stacked bar chart */}
      <div className="rounded-lg border border-border/50 p-4">
        <p className="text-sm font-semibold mb-3">Typical work week</p>
        <div style={{ height: 260 }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border) / 0.5)" />
              <XAxis dataKey="day" tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }} />
              <YAxis
                tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
                tickFormatter={v => v === 0 ? '0' : `${Math.floor(v)}h`}
                width={35}
              />
              <Tooltip content={<CustomTooltip />} />
              <Legend iconSize={10} iconType="circle" wrapperStyle={{ fontSize: 12 }} />
              <Bar dataKey="work" name="Work" stackId="a" fill="#3b82f6" />
              <Bar dataKey="commute" name="Commute" stackId="a" fill="#22c55e" />
              <Bar dataKey="lunch" name="Lunch" stackId="a" fill="#f97316" />
              <Bar dataKey="idle" name="Idle" stackId="a" fill="hsl(var(--muted-foreground) / 0.3)" radius={[2, 2, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Data table */}
      <div className="rounded-lg border border-border/50 overflow-hidden">
        <div className="px-4 py-3 bg-muted/20 border-b border-border/30">
          <p className="text-sm font-semibold">Average by day (Mon–Fri, work days only)</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border/30 bg-muted/10">
                {['Day', 'Avg Work', 'Commute →Work', 'Commute →Home', 'Avg Lunch', 'Avg Idle', 'Days'].map(h => (
                  <th key={h} className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {[1, 2, 3, 4, 5].map(dow => {
                const g = groups[dow]
                if (!g.work.length) return null
                return (
                  <tr key={dow} className="border-b border-border/20 hover:bg-muted/10">
                    <td className="px-3 py-2 font-medium">{DOW_NAMES[dow]}</td>
                    <td className="px-3 py-2 tabular-nums text-blue-500">{fmtDur(avg(g.work))}</td>
                    <td className="px-3 py-2 tabular-nums text-green-500">{fmtDur(avg(g.commute))}</td>
                    <td className="px-3 py-2 tabular-nums text-green-500">{fmtDur(avg(g.commuteHome))}</td>
                    <td className="px-3 py-2 tabular-nums text-orange-500">{fmtDur(avg(g.lunch))}</td>
                    <td className="px-3 py-2 tabular-nums text-muted-foreground">{fmtDur(avg(g.idle))}</td>
                    <td className="px-3 py-2 tabular-nums text-muted-foreground">{g.work.length}</td>
                  </tr>
                )
              })}
              {[1, 2, 3, 4, 5].every(d => !groups[d].work.length) && (
                <tr>
                  <td colSpan={7} className="px-3 py-4 text-center text-muted-foreground text-sm">No data</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

import { type EmployerAttendanceResponse, type UserSettings } from '@/lib/api'
import { fmtDur, fmtClockTime, DOW_NAMES } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { CheckCircle2, AlertTriangle } from 'lucide-react'

function fmtEmployerDate(isoDate: string): string {
  const d = new Date(isoDate + 'T12:00:00Z')
  return `${DOW_NAMES[d.getUTCDay()]} ${String(d.getUTCDate()).padStart(2,'0')}/${String(d.getUTCMonth()+1).padStart(2,'0')}`
}

interface Props {
  employer: EmployerAttendanceResponse | null
  settings: UserSettings | null
}

export function EmployerTab({ employer, settings }: Props) {
  const data = employer ?? { records: [], lastImport: null, totalRecords: 0 }
  const records = data.records ?? []

  const targetH = settings?.targetOfficeHours
    ? Number(settings.targetOfficeHours)
    : settings?.targetWorkHours ? Number(settings.targetWorkHours) : null

  // Last import
  let lastImportStr = ''
  if (data.lastImport) {
    const d = new Date(data.lastImport)
    const MONTHS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']
    lastImportStr = `${MONTHS[d.getMonth()]} ${String(d.getDate()).padStart(2,'0')}, ${d.getFullYear()}`
  }

  // Reserve calculation
  let reserveEl: React.ReactNode = null
  if (targetH != null) {
    const daysWithData = records.filter(r => r.workingHours != null)
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

  const sorted = records.slice().sort((a, b) => b.date.localeCompare(a.date))

  return (
    <div className="space-y-4">
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

        {records.length === 0 ? (
          <div className="px-4 py-6 text-center text-sm text-muted-foreground">
            No attendance records in this period.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-t border-border/30 bg-muted/10">
                  {['Date', 'Clock In', 'Clock Out', 'Hours', 'Delta', 'Status'].map(h => (
                    <th key={h} className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {sorted.map(r => {
                  const isAbsent = (r.eventTypes ?? '').toLowerCase().includes('absence')
                  const hasConflict = r.hasConflict && !isAbsent

                  const clockIn = isAbsent ? '—' : fmtClockTime(r.clockIn)
                  const clockOut = isAbsent ? '—' : fmtClockTime(r.clockOut)
                  const hoursStr = isAbsent ? '—' : (r.workingHours != null ? fmtDur(r.workingHours) : '—')

                  let deltaEl: React.ReactNode = '—'
                  if (!isAbsent && r.workingHours != null && targetH != null) {
                    const diff = r.workingHours - targetH
                    const absDur = fmtDur(Math.abs(diff))
                    deltaEl = diff >= 0
                      ? <span className="text-green-600 dark:text-green-400">+{absDur}</span>
                      : <span className="text-red-600 dark:text-red-400">−{absDur}</span>
                  }

                  let statusEl: React.ReactNode
                  if (isAbsent) {
                    statusEl = <span className="text-muted-foreground text-xs">No data</span>
                  } else if (hasConflict) {
                    statusEl = (
                      <span className="inline-flex items-center gap-1 text-xs px-1.5 py-0.5 rounded bg-yellow-500/10 text-yellow-600 dark:text-yellow-400 border border-yellow-500/30">
                        <AlertTriangle className="h-3 w-3" />
                        {r.conflictType ?? 'flagged'}
                      </span>
                    )
                  } else {
                    statusEl = <CheckCircle2 className="h-4 w-4 text-green-600 dark:text-green-400" />
                  }

                  return (
                    <tr
                      key={r.date}
                      className={cn(
                        'border-b border-border/20 hover:bg-muted/10',
                        hasConflict && 'bg-yellow-500/5'
                      )}
                    >
                      <td className="px-3 py-2 font-medium">{fmtEmployerDate(r.date)}</td>
                      <td className="px-3 py-2 font-mono text-xs">{clockIn}</td>
                      <td className="px-3 py-2 font-mono text-xs">{clockOut}</td>
                      <td className="px-3 py-2 tabular-nums">{hoursStr}</td>
                      <td className="px-3 py-2 tabular-nums">{deltaEl}</td>
                      <td className="px-3 py-2">{statusEl}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

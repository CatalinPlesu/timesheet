import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  fetchEntriesForRange, fetchViolations, deleteEntry, updateEntry,
  type Entry, type Violation
} from '@/lib/api'
import { useRequireAuth } from '@/hooks/useAuth'
import { fmtDur, fmtLocalDateTime, localDateISO, fmtDayLabel } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle
} from '@/components/ui/dialog'
import {
  ChevronLeft, ChevronRight, Loader2, Pencil, Trash2, AlertTriangle, Info
} from 'lucide-react'

// ─── Types ────────────────────────────────────────────────────────────────────

type PeriodType = 'Day' | 'Week' | 'Month' | 'Year' | 'All'
type DowFilter = 'All' | 'Mon' | 'Tue' | 'Wed' | 'Thu' | 'Fri' | 'Sat' | 'Sun'
type TypeFilter = 'All' | 'Working' | 'Commuting' | 'Lunch'

// ─── Period range helpers ─────────────────────────────────────────────────────

function getPeriodRange(type: PeriodType, offset: number): { start: string; end: string; label: string } {
  const now = new Date()
  let start: Date, end: Date, label: string

  if (type === 'Day') {
    start = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + offset))
    end = new Date(start)
    end.setUTCDate(start.getUTCDate() + 1)
    label = start.toISOString().slice(0, 10) + ' (' + ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'][start.getUTCDay()] + ')'
  } else if (type === 'Week') {
    const day = now.getUTCDay()
    const monday = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - (day === 0 ? 6 : day - 1) + offset * 7))
    const sunday = new Date(monday)
    sunday.setUTCDate(monday.getUTCDate() + 7)
    start = monday; end = sunday
    label = `${monday.toISOString().slice(0, 10)} – ${new Date(sunday.getTime() - 1).toISOString().slice(0, 10)}`
  } else if (type === 'Month') {
    const y = now.getUTCFullYear(), m = now.getUTCMonth() + offset
    start = new Date(Date.UTC(y, m, 1))
    end = new Date(Date.UTC(y, m + 1, 1))
    label = start.toLocaleString('en', { month: 'long', year: 'numeric', timeZone: 'UTC' })
  } else if (type === 'Year') {
    const y = now.getUTCFullYear() + offset
    start = new Date(Date.UTC(y, 0, 1))
    end = new Date(Date.UTC(y + 1, 0, 1))
    label = String(y)
  } else {
    start = new Date(Date.UTC(2020, 0, 1))
    end = new Date(Date.UTC(now.getUTCFullYear() + 1, 0, 1))
    label = 'All time'
  }

  return {
    start: start.toISOString().slice(0, 10),
    end: end.toISOString().slice(0, 10),
    label,
  }
}

// ─── State badge ──────────────────────────────────────────────────────────────

function StateBadge({ state }: { state: string }) {
  const s = state.toLowerCase()
  return (
    <Badge
      variant="outline"
      className={cn(
        'text-xs font-medium',
        s === 'working' && 'border-blue-500/50 bg-blue-500/10 text-blue-600 dark:text-blue-400',
        s === 'commuting' && 'border-green-500/50 bg-green-500/10 text-green-600 dark:text-green-400',
        s === 'lunch' && 'border-orange-500/50 bg-orange-500/10 text-orange-600 dark:text-orange-400',
      )}
    >
      {state}
    </Badge>
  )
}

// ─── Adjustment row ───────────────────────────────────────────────────────────

interface AdjustRowProps {
  entryId: string
  which: 'start' | 'end'
  onApply: (id: string, which: 'start' | 'end', minutes: number) => void
  isLoading: boolean
}

function AdjustRow({ entryId, which, onApply, isLoading }: AdjustRowProps) {
  const steps = [-30, -5, -1, 1, 5, 30]
  return (
    <div className="flex items-center gap-1.5 flex-wrap">
      <span className="text-xs text-muted-foreground w-10 capitalize">{which}:</span>
      {steps.map(s => (
        <button
          key={s}
          onClick={() => onApply(entryId, which, s)}
          disabled={isLoading}
          className="text-xs px-2 py-1 rounded border border-border hover:bg-accent transition-colors disabled:opacity-50"
        >
          {s > 0 ? `+${s}m` : `${s}m`}
        </button>
      ))}
    </div>
  )
}

// ─── Edit panel ───────────────────────────────────────────────────────────────

interface EditPanelProps {
  entry: Entry
  onApply: (id: string, which: 'start' | 'end', minutes: number) => void
  isLoading: boolean
}

function EditPanel({ entry, onApply, isLoading }: EditPanelProps) {
  return (
    <tr>
      <td colSpan={5} className="px-4 py-3 bg-muted/30">
        <div className="space-y-2">
          <p className="text-xs text-muted-foreground">
            {fmtLocalDateTime(entry.startedAt)} – {entry.endedAt ? fmtLocalDateTime(entry.endedAt) : 'active'}
          </p>
          <AdjustRow entryId={entry.id} which="start" onApply={onApply} isLoading={isLoading} />
          {!entry.isActive && (
            <AdjustRow entryId={entry.id} which="end" onApply={onApply} isLoading={isLoading} />
          )}
        </div>
      </td>
    </tr>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function EntriesPage() {
  useRequireAuth()

  const [periodType, setPeriodType] = useState<PeriodType>('Week')
  const [offset, setOffset] = useState(0)
  const [typeFilter, setTypeFilter] = useState<TypeFilter>('All')
  const [dowFilter, setDowFilter] = useState<DowFilter>('All')
  const [sortNewest, setSortNewest] = useState(true)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const queryClient = useQueryClient()
  const { start, end, label } = getPeriodRange(periodType, offset)

  const { data: entriesData, isLoading: entriesLoading } = useQuery({
    queryKey: ['entries', start, end],
    queryFn: () => fetchEntriesForRange(start, end),
  })

  const { data: violationsData } = useQuery({
    queryKey: ['violations', start, end],
    queryFn: () => fetchViolations(start, end),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteEntry(id),
    onSuccess: () => {
      setDeleteId(null)
      queryClient.invalidateQueries({ queryKey: ['entries', start, end] })
    },
  })

  const adjustMutation = useMutation({
    mutationFn: ({ id, which, minutes }: { id: string; which: 'start' | 'end'; minutes: number }) =>
      updateEntry(id, which === 'start' ? { startMinutes: minutes } : { endMinutes: minutes }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['entries', start, end] })
    },
  })

  const handleApplyAdjust = (id: string, which: 'start' | 'end', minutes: number) => {
    adjustMutation.mutate({ id, which, minutes })
  }

  const handlePeriodType = (type: PeriodType) => {
    setPeriodType(type)
    setOffset(0)
    setDowFilter('All')
  }

  // Build violation map
  const violationMap = new Map<string, Violation>()
  for (const v of (violationsData?.violations ?? [])) {
    violationMap.set(v.date, v)
  }

  // Sort and filter entries
  let entries = (entriesData?.entries ?? []).slice()
    .sort((a, b) => {
      const diff = new Date(a.startedAt).getTime() - new Date(b.startedAt).getTime()
      return sortNewest ? -diff : diff
    })

  if (typeFilter !== 'All') {
    entries = entries.filter(e => e.state?.toLowerCase() === typeFilter.toLowerCase())
  }

  if (dowFilter !== 'All') {
    const dowIndex = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].indexOf(dowFilter)
    entries = entries.filter(e => {
      if (dowIndex < 0) return true
      const d = new Date(e.startedAt)
      return d.getDay() === dowIndex
    })
  }

  // Group by local date
  const groups = new Map<string, Entry[]>()
  for (const e of entries) {
    const key = localDateISO(e.startedAt)
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key)!.push(e)
  }

  const showDowFilter = ['Month', 'Year', 'All'].includes(periodType)
  const showNav = periodType !== 'All'

  const periodTypes: PeriodType[] = ['Day', 'Week', 'Month', 'Year', 'All']
  const typeFilters: TypeFilter[] = ['All', 'Working', 'Commuting', 'Lunch']
  const dowFilters: DowFilter[] = ['All', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Entries</h1>
        <p className="text-sm text-muted-foreground mt-0.5">Browse and edit your tracking sessions</p>
      </div>

      {/* Filter bars */}
      <div className="flex flex-wrap gap-2">
        {/* Period type */}
        <div className="flex items-center gap-1 rounded-lg border border-border/50 p-1 bg-muted/30">
          {periodTypes.map(t => (
            <button
              key={t}
              onClick={() => handlePeriodType(t)}
              className={cn(
                'px-2.5 py-1 text-xs rounded-md font-medium transition-colors',
                periodType === t
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'
              )}
            >
              {t}
            </button>
          ))}
        </div>

        {/* Type filter */}
        <div className="flex items-center gap-1 rounded-lg border border-border/50 p-1 bg-muted/30">
          {typeFilters.map(t => (
            <button
              key={t}
              onClick={() => setTypeFilter(t)}
              className={cn(
                'px-2.5 py-1 text-xs rounded-md font-medium transition-colors',
                typeFilter === t
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'
              )}
            >
              {t}
            </button>
          ))}
        </div>

        {/* Sort */}
        <div className="flex items-center gap-1 rounded-lg border border-border/50 p-1 bg-muted/30">
          <button
            onClick={() => setSortNewest(true)}
            className={cn(
              'px-2.5 py-1 text-xs rounded-md font-medium transition-colors',
              sortNewest ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
            )}
          >
            Newest
          </button>
          <button
            onClick={() => setSortNewest(false)}
            className={cn(
              'px-2.5 py-1 text-xs rounded-md font-medium transition-colors',
              !sortNewest ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
            )}
          >
            Oldest
          </button>
        </div>
      </div>

      {/* Day-of-week filter (only for Month/Year/All) */}
      {showDowFilter && (
        <div className="flex items-center gap-1 rounded-lg border border-border/50 p-1 bg-muted/30 w-fit flex-wrap">
          <span className="text-xs text-muted-foreground px-1">Day:</span>
          {dowFilters.map(d => (
            <button
              key={d}
              onClick={() => setDowFilter(d)}
              className={cn(
                'px-2 py-1 text-xs rounded-md font-medium transition-colors',
                dowFilter === d
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'
              )}
            >
              {d}
            </button>
          ))}
        </div>
      )}

      {/* Period navigation */}
      <div className="flex items-center gap-2">
        {showNav && (
          <Button variant="outline" size="icon" className="h-7 w-7" onClick={() => setOffset(o => o - 1)}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
        )}
        <span className="text-sm font-medium flex-1 text-center sm:text-left">{label}</span>
        {showNav && (
          <Button
            variant="outline"
            size="icon"
            className="h-7 w-7"
            disabled={offset >= 0}
            onClick={() => setOffset(o => o + 1)}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Table */}
      {entriesLoading ? (
        <div className="flex items-center gap-2 text-muted-foreground py-8 justify-center">
          <Loader2 className="h-5 w-5 animate-spin" />
          <span>Loading…</span>
        </div>
      ) : groups.size === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <p>No entries for this period.</p>
        </div>
      ) : (
        <div className="rounded-lg border border-border/50 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/50 bg-muted/30">
                  <th className="px-3 py-2.5 text-left font-medium text-muted-foreground">Type</th>
                  <th className="px-3 py-2.5 text-left font-medium text-muted-foreground">Started</th>
                  <th className="px-3 py-2.5 text-left font-medium text-muted-foreground">Ended</th>
                  <th className="px-3 py-2.5 text-left font-medium text-muted-foreground">Duration</th>
                  <th className="px-3 py-2.5 text-right font-medium text-muted-foreground"></th>
                </tr>
              </thead>
              <tbody>
                {[...groups.entries()].map(([dayKey, dayEntries]) => {
                  const violation = violationMap.get(dayKey)
                  return [
                    // Day separator row
                    <tr key={`sep-${dayKey}`} className="border-b border-border/50 bg-muted/20">
                      <td colSpan={5} className="px-3 py-2">
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                            {fmtDayLabel(dayKey)}
                          </span>
                          {violation && (
                            <span
                              className="inline-flex items-center gap-1 text-xs px-1.5 py-0.5 rounded bg-yellow-500/10 text-yellow-600 dark:text-yellow-400 border border-yellow-500/30"
                              title={violation.description}
                            >
                              <AlertTriangle className="h-3 w-3" />
                              {fmtDur(violation.actualHours ?? 0)} actual
                            </span>
                          )}
                        </div>
                      </td>
                    </tr>,
                    // Entry rows
                    ...dayEntries.map(entry => [
                      <tr
                        key={entry.id}
                        className={cn(
                          'border-b border-border/30 transition-colors hover:bg-muted/20',
                          entry.isActive && 'bg-primary/5'
                        )}
                      >
                        <td className="px-3 py-2.5">
                          <div className="flex items-center gap-1.5">
                            <StateBadge state={entry.state} />
                            {entry.note && (
                              <span title={entry.note} className="text-muted-foreground">
                                <Info className="h-3 w-3" />
                              </span>
                            )}
                          </div>
                        </td>
                        <td className="px-3 py-2.5 text-muted-foreground font-mono text-xs">
                          {fmtLocalDateTime(entry.startedAt)}
                        </td>
                        <td className="px-3 py-2.5 text-muted-foreground font-mono text-xs">
                          {entry.endedAt ? fmtLocalDateTime(entry.endedAt) : (
                            <span className="text-primary font-medium">active</span>
                          )}
                        </td>
                        <td className="px-3 py-2.5 font-medium">
                          {entry.durationHours != null ? fmtDur(entry.durationHours) : '—'}
                        </td>
                        <td className="px-3 py-2.5">
                          <div className="flex items-center justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-7 w-7 text-muted-foreground hover:text-foreground"
                              onClick={() => setEditingId(editingId === entry.id ? null : entry.id)}
                            >
                              <Pencil className="h-3.5 w-3.5" />
                            </Button>
                            {!entry.isActive && (
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-7 w-7 text-muted-foreground hover:text-destructive"
                                onClick={() => setDeleteId(entry.id)}
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>,
                      // Edit panel
                      editingId === entry.id && (
                        <EditPanel
                          key={`edit-${entry.id}`}
                          entry={entry}
                          onApply={handleApplyAdjust}
                          isLoading={adjustMutation.isPending}
                        />
                      ),
                    ].filter(Boolean))
                  ]
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Delete confirmation dialog */}
      <Dialog open={deleteId !== null} onOpenChange={open => !open && setDeleteId(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete entry?</DialogTitle>
            <DialogDescription>
              This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2">
            <Button variant="outline" onClick={() => setDeleteId(null)}>Cancel</Button>
            <Button
              variant="destructive"
              disabled={deleteMutation.isPending}
              onClick={() => deleteId && deleteMutation.mutate(deleteId)}
            >
              {deleteMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

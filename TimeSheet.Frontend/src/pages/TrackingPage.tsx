import { useState, useEffect, useRef, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { fetchCurrentState, toggleState, fetchBreakdown, type TrackingState } from '@/lib/api'
import { useRequireAuth } from '@/hooks/useAuth'
import { fmtDur, todayLocalISO } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { Card, CardContent } from '@/components/ui/card'
import { Loader2, Navigation, Briefcase, Utensils } from 'lucide-react'

// ─── Offset picker popup ─────────────────────────────────────────────────────

interface OffsetPickerProps {
  onSelect: (offset: number) => void
  onClose: () => void
}

function OffsetPicker({ onSelect, onClose }: OffsetPickerProps) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose()
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [onClose])

  const presets = [
    { label: '-30m', value: -30 },
    { label: '-15m', value: -15 },
    { label: '-5m', value: -5 },
    { label: '-1m', value: -1 },
    { label: 'Now', value: 0 },
    { label: '+1m', value: 1 },
    { label: '+5m', value: 5 },
    { label: '+15m', value: 15 },
    { label: '+30m', value: 30 },
  ]

  return (
    <div
      ref={ref}
      className="absolute top-full mt-2 left-1/2 -translate-x-1/2 z-50
        bg-popover border border-border rounded-lg shadow-xl p-2
        flex flex-wrap gap-1 w-56"
    >
      <p className="w-full text-xs text-muted-foreground text-center pb-1 font-medium">Start offset</p>
      {presets.map(p => (
        <button
          key={p.value}
          onClick={() => { onSelect(p.value); onClose() }}
          className={cn(
            'flex-1 min-w-[calc(33%-4px)] text-xs px-2 py-1.5 rounded-md font-medium transition-colors',
            p.value === 0
              ? 'bg-primary text-primary-foreground'
              : 'bg-secondary hover:bg-secondary/80 text-secondary-foreground'
          )}
        >
          {p.label}
        </button>
      ))}
    </div>
  )
}

// ─── Track button ─────────────────────────────────────────────────────────────

interface TrackButtonProps {
  state: TrackingState
  label: string
  icon: React.ReactNode
  colorClass: string
  activeColorClass: string
  isActive: boolean
  isLoading: boolean
  onToggle: (state: TrackingState, offset?: number) => void
}

function TrackButton({ state, label, icon, colorClass, activeColorClass, isActive, isLoading, onToggle }: TrackButtonProps) {
  const [showPicker, setShowPicker] = useState(false)
  const longPressTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const didLongPress = useRef(false)

  const handlePointerDown = () => {
    didLongPress.current = false
    longPressTimer.current = setTimeout(() => {
      didLongPress.current = true
      setShowPicker(true)
    }, 500)
  }

  const handlePointerUp = () => {
    if (longPressTimer.current) clearTimeout(longPressTimer.current)
  }

  const handleClick = () => {
    if (didLongPress.current) return
    onToggle(state, 0)
  }

  const handleContextMenu = (e: React.MouseEvent) => {
    e.preventDefault()
    setShowPicker(v => !v)
  }

  return (
    <div className="relative">
      <button
        onPointerDown={handlePointerDown}
        onPointerUp={handlePointerUp}
        onClick={handleClick}
        onContextMenu={handleContextMenu}
        disabled={isLoading}
        className={cn(
          'relative w-full flex flex-col items-center justify-center gap-2',
          'h-32 sm:h-36 rounded-2xl border-2 transition-all duration-200',
          'font-semibold text-sm select-none touch-none',
          'focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
          isActive
            ? cn(activeColorClass, 'shadow-lg scale-[1.02]')
            : cn(colorClass, 'hover:scale-[1.01] active:scale-[0.99]'),
          isLoading && 'opacity-60 cursor-not-allowed'
        )}
      >
        {isLoading ? (
          <Loader2 className="h-6 w-6 animate-spin" />
        ) : (
          <>
            <span className="h-7 w-7">{icon}</span>
            <span className="text-base font-semibold">{label}</span>
            {isActive && (
              <span className="absolute top-2.5 right-2.5 h-2.5 w-2.5 rounded-full bg-current animate-pulse" />
            )}
          </>
        )}
      </button>
      {showPicker && (
        <OffsetPicker
          onSelect={offset => onToggle(state, offset)}
          onClose={() => setShowPicker(false)}
        />
      )}
    </div>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function TrackingPage() {
  useRequireAuth()
  const queryClient = useQueryClient()
  const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null)
  const messageTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const today = todayLocalISO()
  const tomorrow = (() => {
    const d = new Date(today + 'T12:00:00Z')
    d.setUTCDate(d.getUTCDate() + 1)
    return d.toISOString().slice(0, 10)
  })()

  const { data, isLoading } = useQuery({
    queryKey: ['currentState'],
    queryFn: fetchCurrentState,
    refetchInterval: 30000,
  })

  const { data: todayBreakdown } = useQuery({
    queryKey: ['breakdown', today, tomorrow],
    queryFn: () => fetchBreakdown(today, tomorrow),
    refetchInterval: 60000,
  })

  const mutation = useMutation({
    mutationFn: ({ state, offset }: { state: TrackingState; offset?: number }) =>
      toggleState(state, offset ?? 0),
    onSuccess: result => {
      if (result?.message) {
        showMessage(result.message, 'success')
      }
      queryClient.invalidateQueries({ queryKey: ['currentState'] })
      queryClient.invalidateQueries({ queryKey: ['breakdown', today, tomorrow] })
    },
    onError: () => showMessage('Request failed.', 'error'),
  })

  const showMessage = useCallback((text: string, type: 'success' | 'error') => {
    setMessage({ text, type })
    if (messageTimer.current) clearTimeout(messageTimer.current)
    messageTimer.current = setTimeout(() => setMessage(null), 4000)
  }, [])

  const handleToggle = (state: TrackingState, offset?: number) => {
    mutation.mutate({ state, offset })
  }

  const currentState = data?.state ?? 'Idle'
  const durationHours = data?.durationHours ?? 0

  // Get today's summary from breakdown
  const todaySummary = todayBreakdown?.[0]

  const buttons: Array<{
    state: TrackingState
    label: string
    icon: React.ReactNode
    colorClass: string
    activeColorClass: string
  }> = [
    {
      state: 'Commuting',
      label: 'Commute',
      icon: <Navigation className="h-full w-full" />,
      colorClass: 'border-green-500/30 bg-green-500/5 text-green-600 dark:text-green-400 hover:bg-green-500/10 hover:border-green-500/50',
      activeColorClass: 'border-green-500 bg-green-500/20 text-green-600 dark:text-green-400 shadow-green-500/20',
    },
    {
      state: 'Working',
      label: 'Work',
      icon: <Briefcase className="h-full w-full" />,
      colorClass: 'border-blue-500/30 bg-blue-500/5 text-blue-600 dark:text-blue-400 hover:bg-blue-500/10 hover:border-blue-500/50',
      activeColorClass: 'border-blue-500 bg-blue-500/20 text-blue-600 dark:text-blue-400 shadow-blue-500/20',
    },
    {
      state: 'Lunch',
      label: 'Lunch',
      icon: <Utensils className="h-full w-full" />,
      colorClass: 'border-orange-500/30 bg-orange-500/5 text-orange-600 dark:text-orange-400 hover:bg-orange-500/10 hover:border-orange-500/50',
      activeColorClass: 'border-orange-500 bg-orange-500/20 text-orange-600 dark:text-orange-400 shadow-orange-500/20',
    },
  ]

  return (
    <div className="max-w-lg mx-auto space-y-5">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Tracking</h1>
        <p className="text-sm text-muted-foreground mt-0.5">
          Long-press or right-click a button to set a time offset
        </p>
      </div>

      {/* Current state + today summary */}
      <Card className="border-border/50">
        <CardContent className="pt-5 pb-5">
          {isLoading ? (
            <div className="flex items-center gap-2 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm">Loading…</span>
            </div>
          ) : (
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs text-muted-foreground uppercase tracking-wide mb-1 font-medium">Current state</p>
                <p className={cn(
                  'text-3xl font-bold tracking-tight',
                  currentState === 'Working' && 'text-blue-500',
                  currentState === 'Commuting' && 'text-green-500',
                  currentState === 'Lunch' && 'text-orange-500',
                  currentState === 'Idle' && 'text-muted-foreground',
                )}>
                  {currentState}
                </p>
                {currentState !== 'Idle' && (durationHours ?? 0) > 0 && (
                  <p className="text-sm text-muted-foreground mt-1">
                    for <span className="font-medium text-foreground">{fmtDur(durationHours)}</span>
                  </p>
                )}
              </div>

              {/* Today summary from breakdown */}
              {todaySummary && (
                <div className="text-right space-y-1">
                  <p className="text-xs text-muted-foreground font-medium">Today</p>
                  {(todaySummary.workHours ?? 0) > 0 && (
                    <p className="text-sm"><span className="text-blue-500 font-semibold">{fmtDur(todaySummary.workHours)}</span> <span className="text-muted-foreground">work</span></p>
                  )}
                  {((todaySummary.commuteToWorkHours ?? 0) + (todaySummary.commuteToHomeHours ?? 0)) > 0 && (
                    <p className="text-sm"><span className="text-green-500 font-semibold">{fmtDur((todaySummary.commuteToWorkHours ?? 0) + (todaySummary.commuteToHomeHours ?? 0))}</span> <span className="text-muted-foreground">commute</span></p>
                  )}
                  {(todaySummary.lunchHours ?? 0) > 0 && (
                    <p className="text-sm"><span className="text-orange-500 font-semibold">{fmtDur(todaySummary.lunchHours)}</span> <span className="text-muted-foreground">lunch</span></p>
                  )}
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Messages */}
      {message && (
        <div className={cn(
          'rounded-lg px-4 py-3 text-sm font-medium',
          message.type === 'success'
            ? 'bg-green-500/10 text-green-600 dark:text-green-400 border border-green-500/20'
            : 'bg-destructive/10 text-destructive border border-destructive/20'
        )}>
          {message.text}
        </div>
      )}

      {/* Toggle buttons */}
      <div className="grid grid-cols-3 gap-3">
        {buttons.map(btn => (
          <TrackButton
            key={btn.state}
            {...btn}
            isActive={currentState === btn.state}
            isLoading={mutation.isPending}
            onToggle={handleToggle}
          />
        ))}
      </div>

      <p className="text-xs text-center text-muted-foreground/50">
        Long-press or right-click for time offset
      </p>
    </div>
  )
}

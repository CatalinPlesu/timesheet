import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  fetchStats, fetchBreakdown, fetchPeriodAggregate, fetchViolations,
  fetchEmployerAttendance, fetchSettings, dateRange
} from '@/lib/api'
import { useRequireAuth } from '@/hooks/useAuth'
import { cn } from '@/lib/utils'
import { Loader2 } from 'lucide-react'
import { StatsTab } from './analytics/StatsTab'
import { ChartTab } from './analytics/ChartTab'
import { CalendarTab } from './analytics/CalendarTab'
import { CommuteTab } from './analytics/CommuteTab'
import { PatternsTab } from './analytics/PatternsTab'
import { EmployerTab } from './analytics/EmployerTab'

type TabId = 'stats' | 'chart' | 'calendar' | 'commute' | 'patterns' | 'employer'

const TABS: Array<{ id: TabId; label: string }> = [
  { id: 'stats', label: 'Stats' },
  { id: 'chart', label: 'Chart' },
  { id: 'calendar', label: 'Calendar' },
  { id: 'commute', label: 'Commute' },
  { id: 'patterns', label: 'Patterns' },
  { id: 'employer', label: 'Employer' },
]

const PERIOD_OPTIONS = [
  { value: 7, label: '7d' },
  { value: 30, label: '30d' },
  { value: 90, label: '90d' },
  { value: 365, label: '1y' },
  { value: 3650, label: 'All' },
]

export function AnalyticsPage() {
  useRequireAuth()

  const [activeTab, setActiveTab] = useState<TabId>('stats')
  const [period, setPeriod] = useState(30)

  const { startDate, endDate } = dateRange(period)

  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['stats', period],
    queryFn: () => fetchStats(period),
    retry: false,
  })

  const { data: breakdown = [], isLoading: breakdownLoading } = useQuery({
    queryKey: ['breakdown', startDate, endDate],
    queryFn: () => fetchBreakdown(startDate, endDate),
    retry: false,
  })

  const { data: periodAggregate } = useQuery({
    queryKey: ['periodAggregate', startDate, endDate],
    queryFn: () => fetchPeriodAggregate(startDate, endDate),
    retry: false,
  })

  const { data: violations } = useQuery({
    queryKey: ['violations', startDate, endDate],
    queryFn: () => fetchViolations(startDate, endDate),
    retry: false,
  })

  const { data: employer } = useQuery({
    queryKey: ['employerAttendance', startDate, endDate],
    queryFn: () => fetchEmployerAttendance(startDate, endDate),
    retry: false,
  })

  const { data: settings } = useQuery({
    queryKey: ['settings'],
    queryFn: fetchSettings,
    staleTime: 5 * 60 * 1000,
    retry: false,
  })

  const isLoading = statsLoading || breakdownLoading

  const renderTab = () => {
    if (isLoading && activeTab !== 'chart' && activeTab !== 'calendar' && activeTab !== 'commute') {
      return (
        <div className="flex items-center justify-center py-16 text-muted-foreground">
          <Loader2 className="h-6 w-6 animate-spin" />
        </div>
      )
    }

    switch (activeTab) {
      case 'stats':
        return (
          <StatsTab
            stats={stats ?? null}
            breakdown={breakdown}
            periodAggregate={periodAggregate ?? null}
            violations={violations ?? null}
            anaPeriod={period}
          />
        )
      case 'chart':
        return <ChartTab breakdown={breakdown} employer={employer ?? null} />
      case 'calendar':
        return <CalendarTab employer={employer ?? null} />
      case 'commute':
        return <CommuteTab breakdown={breakdown} />
      case 'patterns':
        return <PatternsTab breakdown={breakdown} />
      case 'employer':
        return <EmployerTab employer={employer ?? null} settings={settings ?? null} />
      default:
        return null
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Analytics</h1>
        <p className="text-sm text-muted-foreground mt-0.5">Work hour statistics and patterns</p>
      </div>

      {/* Period selector */}
      <div className="flex items-center gap-1 rounded-lg border border-border/50 p-1 bg-muted/30 w-fit">
        {PERIOD_OPTIONS.map(opt => (
          <button
            key={opt.value}
            onClick={() => setPeriod(opt.value)}
            className={cn(
              'px-3 py-1.5 text-xs rounded-md font-medium transition-colors',
              period === opt.value
                ? 'bg-background text-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground'
            )}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {/* Tab navigation */}
      <div className="border-b border-border/50">
        <div className="flex gap-0 overflow-x-auto">
          {TABS.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                'px-4 py-2 text-sm font-medium whitespace-nowrap transition-colors border-b-2 -mb-px',
                activeTab === tab.id
                  ? 'border-primary text-foreground'
                  : 'border-transparent text-muted-foreground hover:text-foreground hover:border-border'
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Tab content */}
      <div>
        {renderTab()}
      </div>
    </div>
  )
}

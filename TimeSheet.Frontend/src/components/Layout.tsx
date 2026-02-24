import { NavLink, useNavigate } from 'react-router-dom'
import { Moon, Sun, LogOut, Clock, BarChart3, List } from 'lucide-react'
import { auth } from '@/lib/api'
import { useTheme } from '@/lib/theme'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'

interface LayoutProps {
  children: React.ReactNode
}

export function Layout({ children }: LayoutProps) {
  const navigate = useNavigate()
  const { theme, toggleTheme } = useTheme()

  const handleLogout = () => {
    auth.clearToken()
    navigate('/login', { replace: true })
  }

  const navItems = [
    { to: '/', label: 'Tracking', icon: Clock },
    { to: '/entries', label: 'Entries', icon: List },
    { to: '/analytics', label: 'Analytics', icon: BarChart3 },
  ]

  return (
    <div className="flex flex-col min-h-screen bg-background">
      {/* Top navigation bar */}
      <header className="sticky top-0 z-50 w-full border-b border-border/50 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container mx-auto flex h-14 max-w-screen-xl items-center px-4">
          {/* Logo + brand */}
          <div className="flex items-center gap-2.5 mr-6">
            <img src="/logo.png" alt="TimeSheet" className="h-7 w-auto" />
            <span className="font-semibold text-base tracking-tight hidden sm:inline">TimeSheet</span>
          </div>

          {/* Nav links */}
          <nav className="flex items-center gap-1 flex-1">
            {navItems.map(({ to, label, icon: Icon }) => (
              <NavLink
                key={to}
                to={to}
                end={to === '/'}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-accent text-accent-foreground'
                      : 'text-muted-foreground hover:text-foreground hover:bg-accent/50'
                  )
                }
              >
                <Icon className="h-4 w-4" />
                <span className="hidden sm:inline">{label}</span>
              </NavLink>
            ))}
          </nav>

          {/* Right side controls */}
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="icon"
              onClick={toggleTheme}
              title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
              className="h-8 w-8 text-muted-foreground hover:text-foreground"
            >
              {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={handleLogout}
              title="Logout"
              className="h-8 w-8 text-muted-foreground hover:text-foreground"
            >
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </header>

      {/* Page content */}
      <main className="flex-1 container mx-auto max-w-screen-xl px-4 py-6">
        {children}
      </main>
    </div>
  )
}

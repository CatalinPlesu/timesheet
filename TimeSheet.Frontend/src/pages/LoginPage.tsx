import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useTheme } from '@/lib/theme'
import { apiLogin, auth } from '@/lib/api'
import { useRedirectIfLoggedIn } from '@/hooks/useAuth'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Moon, Sun, Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

export function LoginPage() {
  useRedirectIfLoggedIn()

  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { theme, toggleTheme } = useTheme()
  const [mnemonic, setMnemonic] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const doLogin = async (phrase: string) => {
    const trimmed = phrase.trim()
    if (!trimmed) return
    setLoading(true)
    setError('')
    try {
      const data = await apiLogin(trimmed)
      auth.saveToken(data.accessToken)
      auth.saveUtcOffset(data.utcOffsetMinutes ?? 0)
      navigate('/', { replace: true })
    } catch {
      setError('Invalid mnemonic. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const handleLogin = () => doLogin(mnemonic)

  // Auto-login when ?m= query param is present
  useEffect(() => {
    const urlMnemonic = searchParams.get('m')
    if (urlMnemonic) {
      // Remove the mnemonic from the URL immediately to prevent it from lingering
      window.history.replaceState(null, '', window.location.pathname)
      doLogin(urlMnemonic)
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      {/* Theme toggle */}
      <div className="absolute top-4 right-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleTheme}
          className="text-muted-foreground hover:text-foreground"
        >
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </Button>
      </div>

      <div className="w-full max-w-sm">
        {/* Logo */}
        <div className="flex flex-col items-center mb-8">
          <img src="/logo.png" alt="TimeSheet" className="h-16 w-auto mb-3" />
          <h1 className="text-2xl font-bold tracking-tight">TimeSheet</h1>
          <p className="text-sm text-muted-foreground mt-1">Personal work-hour tracking</p>
        </div>

        <Card className="border-border/50 shadow-xl shadow-black/10">
          <CardHeader className="space-y-1 pb-4">
            <CardTitle className="text-lg">Sign in</CardTitle>
            <CardDescription>
              Enter the one-time mnemonic phrase from Telegram
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="mnemonic">
                Mnemonic phrase
              </label>
              <input
                type="password"
                id="mnemonic"
                value={mnemonic}
                onChange={e => setMnemonic(e.target.value)}
                onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); handleLogin() } }}
                placeholder="word1 word2 word3 …"
                autoComplete="current-password"
                autoCorrect="off"
                spellCheck={false}
                className={cn(
                  'w-full rounded-md border border-input bg-background px-3 py-2 text-sm',
                  'placeholder:text-muted-foreground',
                  'focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-1 focus:ring-offset-background',
                  'font-mono tracking-wide',
                  error && 'border-destructive focus:ring-destructive'
                )}
              />
              {error && (
                <p className="text-sm text-destructive">{error}</p>
              )}
            </div>

            <Button
              onClick={handleLogin}
              disabled={loading || !mnemonic.trim()}
              className="w-full"
            >
              {loading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Signing in…
                </>
              ) : 'Sign in'}
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

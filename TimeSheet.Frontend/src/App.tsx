import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from '@/lib/theme'
import { Layout } from '@/components/Layout'
import { auth } from '@/lib/api'
import { Loader2 } from 'lucide-react'
import './App.css'

// Lazy-load pages for code splitting
const LoginPage = lazy(() => import('@/pages/LoginPage').then(m => ({ default: m.LoginPage })))
const TrackingPage = lazy(() => import('@/pages/TrackingPage').then(m => ({ default: m.TrackingPage })))
const EntriesPage = lazy(() => import('@/pages/EntriesPage').then(m => ({ default: m.EntriesPage })))
const AnalyticsPage = lazy(() => import('@/pages/AnalyticsPage').then(m => ({ default: m.AnalyticsPage })))

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30 * 1000, // 30s
      retry: 1,
    },
  },
})

function PageLoader() {
  return (
    <div className="flex items-center justify-center py-20 text-muted-foreground">
      <Loader2 className="h-6 w-6 animate-spin" />
    </div>
  )
}

function ProtectedLayout({ children }: { children: React.ReactNode }) {
  if (!auth.isLoggedIn()) {
    return <Navigate to="/login" replace />
  }
  return <Layout>{children}</Layout>
}

function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Suspense fallback={<PageLoader />}>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route
                path="/"
                element={
                  <ProtectedLayout>
                    <TrackingPage />
                  </ProtectedLayout>
                }
              />
              <Route
                path="/entries"
                element={
                  <ProtectedLayout>
                    <EntriesPage />
                  </ProtectedLayout>
                }
              />
              <Route
                path="/analytics"
                element={
                  <ProtectedLayout>
                    <AnalyticsPage />
                  </ProtectedLayout>
                }
              />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Suspense>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App

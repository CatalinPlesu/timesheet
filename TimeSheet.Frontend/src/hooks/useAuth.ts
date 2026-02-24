import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { auth } from '@/lib/api'

/** Redirect to /login if not authenticated */
export function useRequireAuth() {
  const navigate = useNavigate()
  useEffect(() => {
    if (!auth.isLoggedIn()) {
      navigate('/login', { replace: true })
    }
    const onLogout = () => navigate('/login', { replace: true })
    window.addEventListener('ts:logout', onLogout)
    return () => window.removeEventListener('ts:logout', onLogout)
  }, [navigate])
}

/** Redirect to / if already authenticated */
export function useRedirectIfLoggedIn() {
  const navigate = useNavigate()
  useEffect(() => {
    if (auth.isLoggedIn()) {
      navigate('/', { replace: true })
    }
  }, [navigate])
}

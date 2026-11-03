import { useEffect, useState } from 'react'
import AuthPage from './pages/auth/AuthPage'
import type { AuthPageMode } from './pages/auth/AuthPage'

function getRoute(): AuthPageMode {
  if (window.location.hash === '#register' || window.location.hash === '#registo') return 'register'
  if (window.location.hash === '#forgot-password') return 'forgot-password'
  return 'login'
}

function App() {
  const [route, setRoute] = useState<AuthPageMode>(getRoute)

  useEffect(() => {
    const syncRoute = () => setRoute(getRoute())
    window.addEventListener('hashchange', syncRoute)
    return () => window.removeEventListener('hashchange', syncRoute)
  }, [])

  function navigate(next: AuthPageMode) {
    if (next === route) return
    window.location.hash = next
    setRoute(next)
  }

  return <AuthPage page={route} onNavigate={navigate} />
}

export default App

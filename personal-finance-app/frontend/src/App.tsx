import { useEffect, useState } from 'react'
import LoginPage from './pages/auth/LoginPage'
import RegisterPage from './pages/auth/RegisterPage'
import type { AuthPageMode } from './pages/auth/AuthPage'

type ViewTransitionDocument = Document & {
  startViewTransition?: (updateCallback: () => void) => unknown
}

function getRoute(): AuthPageMode {
  return window.location.hash === '#register' ? 'register' : 'login'
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

    const updateRoute = () => {
      window.location.hash = next
      setRoute(next)
    }

    const viewTransitionDocument = document as ViewTransitionDocument
    if (viewTransitionDocument.startViewTransition) {
      viewTransitionDocument.startViewTransition(updateRoute)
      return
    }

    updateRoute()
  }

  return route === 'register'
    ? <RegisterPage key="register" onNavigate={navigate} />
    : <LoginPage key="login" onNavigate={navigate} />
}

export default App

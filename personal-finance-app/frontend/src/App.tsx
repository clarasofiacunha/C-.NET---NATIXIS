import { useEffect, useState } from 'react'
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage'
import LoginPage from './pages/auth/LoginPage'
import RegisterPage from './pages/auth/RegisterPage'
import type { AuthPageMode } from './pages/auth/AuthPage'

function getRoute(): AuthPageMode {
  if (window.location.hash === '#register') return 'register'
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

  if (route === 'register') return <RegisterPage key="register" onNavigate={navigate} />
  if (route === 'forgot-password') return <ForgotPasswordPage key="forgot-password" onNavigate={navigate} />
  return <LoginPage key={route} onNavigate={navigate} />
}

export default App

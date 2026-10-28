import AuthPage from './AuthPage'

interface LoginPageProps {
  onNavigate: (page: 'login' | 'registo') => void
}

function LoginPage({ onNavigate }: LoginPageProps) {
  return <AuthPage page="login" onNavigate={onNavigate} />
}

export default LoginPage

import AuthPage from './AuthPage'

interface RegisterPageProps {
  onNavigate: (page: 'login' | 'register') => void
}

function RegisterPage({ onNavigate }: RegisterPageProps) {
  return <AuthPage page="register" onNavigate={onNavigate} />
}

export default RegisterPage

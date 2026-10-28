import AuthPage from './AuthPage'

interface RegisterPageProps {
  onNavigate: (page: 'login' | 'registo') => void
}

function RegisterPage({ onNavigate }: RegisterPageProps) {
  return <AuthPage page="registo" onNavigate={onNavigate} />
}

export default RegisterPage

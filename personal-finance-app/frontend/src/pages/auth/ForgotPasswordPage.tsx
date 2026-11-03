import AuthPage from './AuthPage'

interface ForgotPasswordPageProps {
  onNavigate: (page: 'login' | 'register' | 'forgot-password') => void
}

function ForgotPasswordPage({ onNavigate }: ForgotPasswordPageProps) {
  return <AuthPage page="forgot-password" onNavigate={onNavigate} />
}

export default ForgotPasswordPage

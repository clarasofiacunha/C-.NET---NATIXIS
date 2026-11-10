import type { AuthPageMode } from './AuthPage'
export default function AuthNavigation({ page, onNavigate }: { page: AuthPageMode; onNavigate: (page: AuthPageMode) => void }) {
  return <nav className="page-switch" aria-label="Authentication">
    <a href="#login" aria-current={page === 'login' ? 'page' : undefined} onClick={event => { event.preventDefault(); onNavigate('login') }}>Sign in</a>
    <a href="#register" aria-current={page === 'register' ? 'page' : undefined} onClick={event => { event.preventDefault(); onNavigate('register') }}>Create account</a>
  </nav>
}

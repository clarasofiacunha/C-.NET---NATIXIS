import type { AuthPageMode } from './AuthPage'
export default function ForgotPasswordPage({ onNavigate }: { onNavigate: (page: AuthPageMode) => void }) {
  return <>
    <nav className="page-switch"><a href="#login" onClick={event => { event.preventDefault(); onNavigate('login') }}>Back to sign in</a></nav>
    <h2 id="form-title">Password recovery</h2>
    <p className="subtitle" role="status">Password recovery is not available yet.</p>
  </>
}

import { useState } from 'react'
import type { FormEvent } from 'react'
import { useEffect } from 'react'
import '../../styles/auth.css'
import { authRequest } from '../../auth'
import type { User } from '../../auth'

export type AuthPageMode = 'login' | 'register' | 'forgot-password'
type Field = 'name' | 'email' | 'password' | 'confirm'
const initial = { name: '', email: '', password: '', confirm: '' }
const passwordRules = [
  { label: 'Between 8 and 100 characters', test: (value: string) => value.length >= 8 && value.length <= 100 },
  { label: 'One uppercase and one lowercase letter', test: (value: string) => /[A-Z]/.test(value) && /[a-z]/.test(value) },
  { label: 'One number', test: (value: string) => /\d/.test(value) },
]
const financialTips = [
  'Set a limit for variable expenses.',
  'Set aside a small amount for savings first.',
  'Review your subscriptions at the beginning of each month.',
]

interface AuthPageProps {
  page: AuthPageMode
  onNavigate: (page: AuthPageMode) => void
}

function AuthPage({ page, onNavigate }: AuthPageProps) {
  const [values, setValues] = useState(initial)
  const [touched, setTouched] = useState<Partial<Record<Field, boolean>>>({})
  const [showPassword, setShowPassword] = useState(false)
  const [success, setSuccess] = useState('')
  const [apiError, setApiError] = useState('')
  const [busy, setBusy] = useState(false)
  const [checking, setChecking] = useState(true)
  const [user, setUser] = useState<User | null>(null)
  useEffect(() => {
    let active = true
    authRequest<User>('me').then(current => { if (active) setUser(current) })
      .catch(() => { /* Sem sessão: apresentar os formulários. */ })
      .finally(() => { if (active) setChecking(false) })
    return () => { active = false }
  }, [])
  const [tipIndex, setTipIndex] = useState(0)
  useEffect(() => {
    const interval = window.setInterval(() => setTipIndex(current => (current + 1) % financialTips.length), 6000)
    return () => window.clearInterval(interval)
  }, [])
  const recovery = page === 'forgot-password'
  const passwordSetup = page !== 'login'
  const register = page === 'register'
  const errors: Record<Field, string> = {
    name: values.name.trim().length < 2 ? 'Introduz o teu nome com pelo menos 2 caracteres.' : '',
    email: !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim()) ? 'Introduz um email válido, como nome@exemplo.pt.' : '',
    password: !values.password ? 'Introduz a tua password.' : register && !passwordRules.every(rule => rule.test(values.password)) ? 'A password deve cumprir todas as regras abaixo.' : '',
    confirm: values.confirm !== values.password || !values.confirm ? 'As passwords têm de ser iguais.' : '',
  }

  function navigate(next: AuthPageMode) {
    if (busy) return
    setApiError('')
    onNavigate(next)
    window.history.replaceState(null, '', `#${next}`)
    setValues(initial)
    setTouched({})
    setShowPassword(false)
    setSuccess('')
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSuccess('')
    if (busy) return
    setApiError('')
    if (recovery) {
      setApiError('Password recovery is not available yet.')
      return
    }
    const fields: Field[] = register ? ['name', 'email', 'password', 'confirm'] : ['email', 'password']
    setTouched(Object.fromEntries(fields.map(field => [field, true])))
    const invalid = fields.find(field => errors[field])
    if (invalid) {
      document.getElementById(invalid)?.focus()
      return
    }
    setBusy(true)
    try {
      if (register) {
        await authRequest<User>('register', { name: values.name.trim(), email: values.email.trim(), password: values.password })
        onNavigate('login')
        window.history.replaceState(null, '', '#login')
        setValues({ ...initial, email: values.email.trim() })
        setTouched({})
        setSuccess('Conta criada com sucesso. Já podes entrar.')
      } else {
        setUser(await authRequest<User>('login', { email: values.email.trim(), password: values.password }))
        setValues(initial)
      }
      setShowPassword(false)
    } catch (error) {
      setApiError(error instanceof Error ? error.message : 'Ocorreu um erro. Tenta novamente.')
    } finally {
      setBusy(false)
    }
  }

  async function logout() {
    setBusy(true)
    setApiError('')
    try {
      await authRequest<void>('logout')
      setUser(null)
      onNavigate('login')
      window.history.replaceState(null, '', '#login')
      setTouched({})
      setSuccess('Sessão terminada.')
    } catch (error) {
      setApiError(error instanceof Error ? error.message : 'Não foi possível terminar a sessão.')
    } finally {
      setBusy(false)
    }
  }

  function field(key: Field, label: string, type: string, autoComplete: string, placeholder: string) {
    const invalid = Boolean(touched[key] && errors[key])
    return <div className="field">
      <label htmlFor={key}>{label}</label>
      <div className={key === 'password' ? 'password-input' : undefined}>
        <input id={key} name={key} type={type} autoComplete={autoComplete} placeholder={placeholder}
          required disabled={busy || recovery} maxLength={key === 'name' ? 150 : key === 'email' ? 256 : passwordSetup ? 100 : undefined}
          value={values[key]} aria-invalid={invalid} aria-describedby={invalid ? `${key}-error` : key === 'password' && passwordSetup ? 'password-rules' : undefined}
          onBlur={() => setTouched(previous => ({ ...previous, [key]: true }))}
          onChange={event => { setValues(previous => ({ ...previous, [key]: event.target.value })); setSuccess(''); setApiError('') }} />
        {key === 'password' && <button className="reveal" type="button" aria-controls="password" aria-pressed={showPassword} onClick={() => setShowPassword(!showPassword)}>{showPassword ? 'Hide' : 'Show'}</button>}
      </div>
      {invalid && <p className="error" id={`${key}-error`}>{errors[key]}</p>}
    </div>
  }

  return <main className={`auth-layout auth-layout--${page}`}>
    <aside className="intro">
      <a className="brand" href="#login" onClick={event => { event.preventDefault(); navigate('login') }}><span className="brand-icon" aria-hidden="true">€</span> Personal Finance</a>
      <div className="intro-copy"><span className="eyebrow">YOUR FINANCES, MADE CLEAR</span><h1>Small steps.<br />More control.</h1><p>Organize your expenses, plan your month, and get closer to your goals.</p><p className="financial-tip" aria-live="polite"><span aria-hidden="true">✦</span>{financialTips[tipIndex]}</p><div className="finance-teaser" aria-label="Example financial overview"><div className="balance-card"><span>Available balance</span><strong>€2,480.00</strong><p><b aria-hidden="true">↗</b> +€248 this month</p><svg className="balance-chart" viewBox="0 0 260 62" aria-hidden="true"><path d="M3 49C28 48 39 39 59 42S91 53 111 38s27-21 45-15 26 20 45 10 32-27 56-23" /><circle cx="3" cy="49" r="3" /><circle cx="111" cy="38" r="3" /><circle cx="201" cy="33" r="3" /><circle cx="257" cy="10" r="3" /></svg></div><div className="finance-card finance-card--budget"><span>Monthly budget</span><strong>72%</strong><i><b /></i></div><div className="finance-card finance-card--goal"><span>Savings goal</span><strong>4 / 6 months</strong></div></div><div className="intro-detail"><span aria-hidden="true">↗</span> One place to keep track of your money.</div></div>
      <div className="intro-footer-spacer" aria-hidden="true" />
    </aside>
    <section className="form-panel" aria-labelledby="form-title">
      <div className="form-container">
        {checking ? <p id="form-title" role="status">Checking your session...</p> : user ? <>
          <h2 id="form-title">Hello, {user.name}.</h2>
          <p className="success" role="status">Signed in successfully.</p>
          <p className="subtitle">{user.email}</p>
          <button className="submit" disabled={busy} onClick={logout}>{busy ? 'Signing out...' : 'Sign out'}</button>
          {apiError && <p className="error" role="alert">{apiError}</p>}
        </> : <>
        {recovery
          ? <nav className="page-switch" aria-label="Password recovery"><a href="#login" onClick={event => { event.preventDefault(); navigate('login') }}>← Back to sign in</a></nav>
          : <nav className="page-switch" aria-label="Authentication"><a href="#login" aria-current={!register ? 'page' : undefined} onClick={event => { event.preventDefault(); navigate('login') }}>Sign in</a><a href="#register" aria-current={register ? 'page' : undefined} onClick={event => { event.preventDefault(); navigate('register') }}>Create account</a></nav>}
        <h2 id="form-title">{recovery ? 'Choose a new password.' : register ? 'Start here.' : 'Welcome back.'}</h2>
        <p className="subtitle">{recovery ? 'Choose a strong new password to keep your account secure.' : register ? 'Create your account and start organizing your finances.' : 'Sign in to keep track of your finances.'}</p>
        <form noValidate onSubmit={submit}>
          {register && field('name', 'Name', 'text', 'name', 'Your name')}
          {!recovery && field('email', 'Email', 'email', 'email', 'name@example.com')}
          {recovery
            ? field('password', 'New password', showPassword ? 'text' : 'password', 'new-password', 'Create a new password')
            : field('password', 'Password', showPassword ? 'text' : 'password', register ? 'new-password' : 'current-password', register ? 'Create a password' : 'Your password')}
          {!register && !recovery && <button className="forgot-password" type="button" onClick={() => navigate('forgot-password')}>Forgot password?</button>}
          {passwordSetup && <ul className="rules" id="password-rules">{passwordRules.map(rule => <li key={rule.label} className={rule.test(values.password) ? 'met' : ''}><span aria-hidden="true">{rule.test(values.password) ? '✓' : '○'}</span> {rule.label}</li>)}</ul>}
          {passwordSetup && field('confirm', recovery ? 'Confirm new password' : 'Confirm password', showPassword ? 'text' : 'password', 'new-password', recovery ? 'Repeat your new password' : 'Repeat your password')}
          <button className="submit" type="submit" disabled={busy || recovery}>{busy ? 'Processing...' : recovery ? 'Update password' : register ? 'Create account' : 'Sign in'} <span aria-hidden="true">→</span></button>
          {success && <p className="success" role="status">{success}</p>}
          {apiError && <p className="error" role="alert">{apiError}</p>}
        </form>
        {!recovery && <p className="alternate">{register ? 'Already have an account?' : "Don't have an account yet?"} <a href={register ? '#login' : '#register'} onClick={event => { event.preventDefault(); navigate(register ? 'login' : 'register') }}>{register ? 'Sign in' : 'Create account'}</a></p>}
        {recovery && <p className="prototype" role="status">Password recovery is not available yet.</p>}
        </>}
      </div>
    </section>
  </main>
}

export default AuthPage

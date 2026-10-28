import { useState } from 'react'
import type { FormEvent } from 'react'
import { useEffect } from 'react'
import '../../styles/auth.css'

export type AuthPageMode = 'login' | 'register' | 'forgot-password'
type Field = 'name' | 'email' | 'password' | 'confirm'
const initial = { name: '', email: '', password: '', confirm: '' }
const passwordRules = [
  { label: 'At least 6 characters', test: (value: string) => value.length >= 6 },
  { label: 'One uppercase and one lowercase letter', test: (value: string) => /[A-Z]/.test(value) && /[a-z]/.test(value) },
  { label: 'One number', test: (value: string) => /\d/.test(value) },
  { label: 'One symbol (e.g. !, @, #)', test: (value: string) => /[^a-zA-Z0-9]/.test(value) },
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
  const [success, setSuccess] = useState(false)
  const [tipIndex, setTipIndex] = useState(0)
  const register = page === 'register'
  const recovery = page === 'forgot-password'
  const passwordSetup = register || recovery
  const errors: Record<Field, string> = {
    name: values.name.trim().length < 2 ? 'Enter your name using at least 2 characters.' : '',
    email: recovery || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim()) ? '' : 'Enter a valid email, such as name@example.com.',
    password: !values.password ? 'Enter your password.' : passwordSetup && !passwordRules.every(rule => rule.test(values.password)) ? 'Your password must meet all the requirements below.' : '',
    confirm: values.confirm !== values.password || !values.confirm ? 'Passwords must match.' : '',
  }

  useEffect(() => {
    const interval = window.setInterval(() => setTipIndex(current => (current + 1) % financialTips.length), 6000)
    return () => window.clearInterval(interval)
  }, [])

  function navigate(next: AuthPageMode) {
    onNavigate(next)
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSuccess(false)
    const fields: Field[] = recovery ? ['password', 'confirm'] : register ? ['name', 'email', 'password', 'confirm'] : ['email', 'password']
    setTouched(Object.fromEntries(fields.map(field => [field, true])))
    const invalid = fields.find(field => errors[field])
    if (invalid) {
      document.getElementById(invalid)?.focus()
      return
    }
    setSuccess(true)
  }

  function field(key: Field, label: string, type: string, autoComplete: string, placeholder: string) {
    const invalid = Boolean(touched[key] && errors[key])
    return <div className="field">
      <label htmlFor={key}>{label}</label>
      <div className={key === 'password' ? 'password-input' : undefined}>
        <input id={key} name={key} type={type} autoComplete={autoComplete} placeholder={placeholder}
          required maxLength={key === 'name' ? 100 : key === 'email' ? 254 : 128}
          value={values[key]} aria-invalid={invalid} aria-describedby={invalid ? `${key}-error` : key === 'password' && passwordSetup ? 'password-rules' : undefined}
          onBlur={() => setTouched(previous => ({ ...previous, [key]: true }))}
          onChange={event => { setValues(previous => ({ ...previous, [key]: event.target.value })); setSuccess(false) }} />
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
          <button className="submit" type="submit">{recovery ? 'Update password' : register ? 'Create account' : 'Sign in'} <span aria-hidden="true">→</span></button>
          {success && <p className="success" role="status">{recovery ? 'Your new password is valid. This prototype does not update your account yet.' : `Your details are valid. This prototype validates the form; ${register ? 'registration' : 'sign-in'} is not connected to the API yet.`}</p>}
        </form>
        {!recovery && <p className="alternate">{register ? 'Already have an account?' : "Don't have an account yet?"} <a href={register ? '#login' : '#register'} onClick={event => { event.preventDefault(); navigate(register ? 'login' : 'register') }}>{register ? 'Sign in' : 'Create account'}</a></p>}
        <p className="prototype">Interface prototype · No data is sent</p>
      </div>
    </section>
  </main>
}

export default AuthPage

import { useState } from 'react'
import type { FormEvent } from 'react'
import { authRequest } from './auth'
import type { AuthPageMode } from './AuthPage'

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const confirmationMessage = 'If an account exists for this email, you will receive instructions to reset your password.'

export default function ForgotPasswordPage({ onNavigate }: { onNavigate: (page: AuthPageMode) => void }) {
  const [email, setEmail] = useState('')
  const [touched, setTouched] = useState(false)
  const [busy, setBusy] = useState(false)
  const [submitted, setSubmitted] = useState(false)
  const [apiError, setApiError] = useState('')
  const emailError = !emailPattern.test(email.trim()) ? 'Enter a valid email address, such as name@example.com.' : ''
  const invalid = touched && Boolean(emailError)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy || submitted) return

    setTouched(true)
    setApiError('')
    if (emailError) return

    setBusy(true)
    try {
      await authRequest('forgot-password', { email: email.trim() })
      setSubmitted(true)
    } catch (error) {
      setApiError(error instanceof Error ? error.message : 'Unable to request password recovery. Please try again.')
    } finally {
      setBusy(false)
    }
  }

  return <>
    <nav className="page-switch">
      <a href="#login" onClick={event => { event.preventDefault(); if (!busy) onNavigate('login') }}>Back to sign in</a>
    </nav>
    <h2 id="form-title">Password recovery</h2>
    <p className="subtitle">Enter your email and we will send you instructions to reset your password.</p>
    {submitted ? (
      <p className="success" role="status">{confirmationMessage}</p>
    ) : (
      <form noValidate onSubmit={submit}>
        <div className="field">
          <label htmlFor="recovery-email">Email</label>
          <input
            id="recovery-email"
            name="email"
            type="email"
            autoComplete="email"
            placeholder="name@example.com"
            required
            maxLength={256}
            disabled={busy}
            value={email}
            aria-invalid={invalid}
            aria-describedby={invalid ? 'recovery-email-error' : undefined}
            onBlur={() => setTouched(true)}
            onChange={event => { setEmail(event.target.value); setApiError('') }}
          />
          {invalid && <p className="error" id="recovery-email-error">{emailError}</p>}
        </div>
        <button className="submit" disabled={busy}>
          {busy ? 'Sending...' : 'Send recovery instructions'}
        </button>
        {apiError && <p className="error" role="alert">{apiError}</p>}
      </form>
    )}
    <p className="alternate">
      Remembered your password?{' '}
      <a href="#login" onClick={event => { event.preventDefault(); if (!busy) onNavigate('login') }}>Sign in</a>
    </p>
  </>
}

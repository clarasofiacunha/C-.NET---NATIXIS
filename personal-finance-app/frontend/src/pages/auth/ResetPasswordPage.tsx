import { useState } from 'react'
import type { FormEvent } from 'react'
import { authRequest } from './auth'
import type { AuthPageMode } from './AuthPage'
import { PasswordRules } from './PasswordRules'
import { passwordRules } from './validationRules'

type ResetLink = { email: string; token: string }

function readResetLink(): ResetLink | null {
  const query = window.location.hash.slice('#reset-password'.length)
  if (!query.startsWith('?')) return null

  const params = new URLSearchParams(query.slice(1))
  const email = params.get('email')?.trim() ?? ''
  const token = params.get('token') ?? ''
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) && token ? { email, token } : null
}

export default function ResetPasswordPage({ onNavigate }: { onNavigate: (page: AuthPageMode) => void }) {
  const [link] = useState(readResetLink)
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [touched, setTouched] = useState({ password: false, confirmation: false })
  const [busy, setBusy] = useState(false)
  const [completed, setCompleted] = useState(false)
  const [apiError, setApiError] = useState('')

  const passwordError = !password
    ? 'Enter a new password.'
    : !passwordRules.every(rule => rule.test(password))
      ? 'Your password must meet all the requirements below.'
      : ''
  const confirmationError = !confirmation || confirmation !== password ? 'Passwords must match.' : ''

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy || completed || !link) return

    setTouched({ password: true, confirmation: true })
    setApiError('')
    if (passwordError || confirmationError) return

    setBusy(true)
    try {
      await authRequest('reset-password', { email: link.email, token: link.token, newPassword: password })
      window.history.replaceState(null, '', '#reset-password')
      setCompleted(true)
    } catch (error) {
      setApiError(error instanceof Error ? error.message : 'Unable to reset your password. Please request a new link.')
    } finally {
      setBusy(false)
    }
  }

  if (!link) {
    return <>
      <h2 id="form-title">Invalid recovery link</h2>
      <p className="subtitle">This link is incomplete or invalid. Request a new password recovery link to continue.</p>
      <button className="submit" type="button" onClick={() => onNavigate('forgot-password')}>Request a new link</button>
      <p className="alternate"><a href="#login" onClick={event => { event.preventDefault(); onNavigate('login') }}>Back to sign in</a></p>
    </>
  }

  if (completed) {
    return <>
      <h2 id="form-title">Password updated</h2>
      <p className="success" role="status">Your password has been updated. You can now sign in with your new password.</p>
      <button className="submit" type="button" onClick={() => onNavigate('login')}>Go to sign in</button>
    </>
  }

  return <>
    <h2 id="form-title">Set a new password</h2>
    <p className="subtitle">Choose a strong password for {link.email}.</p>
    <form noValidate onSubmit={submit}>
      <div className="field">
        <label htmlFor="new-password">New password</label>
        <input id="new-password" name="password" type="password" autoComplete="new-password" required maxLength={100} disabled={busy}
          value={password} aria-invalid={touched.password && Boolean(passwordError)} aria-describedby={touched.password && passwordError ? 'new-password-error' : 'password-rules'}
          onBlur={() => setTouched(current => ({ ...current, password: true }))}
          onChange={event => { setPassword(event.target.value); setApiError('') }} />
        {touched.password && passwordError && <p className="error" id="new-password-error">{passwordError}</p>}
      </div>
      <PasswordRules password={password} />
      <div className="field">
        <label htmlFor="confirm-new-password">Confirm new password</label>
        <input id="confirm-new-password" name="confirmation" type="password" autoComplete="new-password" required maxLength={100} disabled={busy}
          value={confirmation} aria-invalid={touched.confirmation && Boolean(confirmationError)} aria-describedby={touched.confirmation && confirmationError ? 'confirm-new-password-error' : undefined}
          onBlur={() => setTouched(current => ({ ...current, confirmation: true }))}
          onChange={event => { setConfirmation(event.target.value); setApiError('') }} />
        {touched.confirmation && confirmationError && <p className="error" id="confirm-new-password-error">{confirmationError}</p>}
      </div>
      <button className="submit" disabled={busy}>{busy ? 'Updating...' : 'Update password'}</button>
      {apiError && <p className="error" role="alert">{apiError}</p>}
    </form>
    <p className="alternate"><a href="#forgot-password" onClick={event => { event.preventDefault(); if (!busy) onNavigate('forgot-password') }}>Request a new link</a></p>
  </>
}

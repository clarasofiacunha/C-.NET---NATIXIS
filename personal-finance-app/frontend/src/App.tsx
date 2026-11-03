import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { authRequest } from './auth'
import type { User } from './auth'

type Page = 'login' | 'registo'
type Field = 'name' | 'email' | 'password' | 'confirm'
const initial = { name: '', email: '', password: '', confirm: '' }
const passwordRules = [
  { label: 'Entre 8 e 100 caracteres', test: (value: string) => value.length >= 8 && value.length <= 100 },
  { label: 'Uma letra maiúscula e uma minúscula', test: (value: string) => /[A-Z]/.test(value) && /[a-z]/.test(value) },
  { label: 'Um número', test: (value: string) => /\d/.test(value) },
]

function App() {
  const [page, setPage] = useState<Page>(window.location.hash === '#registo' ? 'registo' : 'login')
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
  const register = page === 'registo'
  const errors: Record<Field, string> = {
    name: values.name.trim().length < 2 ? 'Introduz o teu nome com pelo menos 2 caracteres.' : '',
    email: !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim()) ? 'Introduz um email válido, como nome@exemplo.pt.' : '',
    password: !values.password ? 'Introduz a tua password.' : register && !passwordRules.every(rule => rule.test(values.password)) ? 'A password deve cumprir todas as regras abaixo.' : '',
    confirm: values.confirm !== values.password || !values.confirm ? 'As passwords têm de ser iguais.' : '',
  }

  function navigate(next: Page) {
    if (busy) return
    setApiError('')
    setPage(next)
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
        setPage('login')
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
      setPage('login')
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
          required disabled={busy} maxLength={key === 'name' ? 150 : key === 'email' ? 256 : register ? 100 : undefined}
          value={values[key]} aria-invalid={invalid} aria-describedby={invalid ? `${key}-error` : key === 'password' && register ? 'password-rules' : undefined}
          onBlur={() => setTouched(previous => ({ ...previous, [key]: true }))}
          onChange={event => { setValues(previous => ({ ...previous, [key]: event.target.value })); setSuccess(''); setApiError('') }} />
        {key === 'password' && <button className="reveal" type="button" aria-controls="password" aria-pressed={showPassword} onClick={() => setShowPassword(!showPassword)}>{showPassword ? 'Ocultar' : 'Mostrar'}</button>}
      </div>
      {invalid && <p className="error" id={`${key}-error`}>{errors[key]}</p>}
    </div>
  }

  return <main className="auth-layout">
    <aside className="intro">
      <a className="brand" href="#login" onClick={event => { event.preventDefault(); navigate('login') }}><span className="brand-icon" aria-hidden="true">€</span> Personal Finance</a>
      <div className="intro-copy"><span className="eyebrow">AS TUAS FINANÇAS, COM CLAREZA</span><h1>Pequenos passos.<br />Mais controlo.</h1><p>Organiza as tuas despesas, planeia o mês e aproxima-te dos teus objetivos.</p><div className="intro-detail"><span aria-hidden="true">↗</span> Um lugar para acompanhares o teu dinheiro.</div></div>
      <small>Personal Finance · Projeto académico</small>
    </aside>
    <section className="form-panel" aria-labelledby="form-title">
      <div className="form-container">
        {checking ? <p id="form-title" role="status">A verificar a sessão…</p> : user ? <>
          <h2 id="form-title">Olá, {user.name}.</h2>
          <p className="success" role="status">Sessão iniciada com sucesso.</p>
          <p className="subtitle">{user.email}</p>
          <button className="submit" disabled={busy} onClick={logout}>{busy ? 'A sair…' : 'Terminar sessão'}</button>
          {apiError && <p className="error" role="alert">{apiError}</p>}
        </> : <>
        <nav className="page-switch" aria-label="Autenticação"><a href="#login" aria-current={!register ? 'page' : undefined} onClick={event => { event.preventDefault(); navigate('login') }}>Entrar</a><a href="#registo" aria-current={register ? 'page' : undefined} onClick={event => { event.preventDefault(); navigate('registo') }}>Criar conta</a></nav>
        <h2 id="form-title">{register ? 'Começa por aqui.' : 'Bem-vindo de volta.'}</h2>
        <p className="subtitle">{register ? 'Cria a tua conta e começa a organizar as tuas finanças.' : 'Entra na tua conta para acompanhar as tuas finanças.'}</p>
        <form noValidate onSubmit={submit}>
          {register && field('name', 'Nome', 'text', 'name', 'O teu nome')}
          {field('email', 'Email', 'email', 'email', 'nome@exemplo.pt')}
          {field('password', 'Password', showPassword ? 'text' : 'password', register ? 'new-password' : 'current-password', register ? 'Cria uma password' : 'A tua password')}
          {register && <ul className="rules" id="password-rules">{passwordRules.map(rule => <li key={rule.label} className={rule.test(values.password) ? 'met' : ''}><span aria-hidden="true">{rule.test(values.password) ? '✓' : '○'}</span> {rule.label}</li>)}</ul>}
          {register && field('confirm', 'Confirmar password', showPassword ? 'text' : 'password', 'new-password', 'Repete a password')}
          <button className="submit" type="submit" disabled={busy}>{busy ? 'A processar…' : register ? 'Criar conta' : 'Entrar'} <span aria-hidden="true">→</span></button>
          {success && <p className="success" role="status">{success}</p>}{apiError && <p className="error" role="alert">{apiError}</p>}
        </form>
        <p className="alternate">{register ? 'Já tens conta?' : 'Ainda não tens conta?'} <a href={register ? '#login' : '#registo'} onClick={event => { event.preventDefault(); navigate(register ? 'login' : 'registo') }}>{register ? 'Entrar' : 'Criar conta'}</a></p>
        </>}
      </div>
    </section>
  </main>
}

export default App

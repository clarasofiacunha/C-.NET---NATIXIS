import { useState } from 'react'
import type { FormEvent } from 'react'
import { useEffect } from 'react'
import '../../styles/auth.css'

export type AuthPageMode = 'login' | 'registo'
type Field = 'name' | 'email' | 'password' | 'confirm'
const initial = { name: '', email: '', password: '', confirm: '' }
const passwordRules = [
  { label: 'Pelo menos 6 caracteres', test: (value: string) => value.length >= 6 },
  { label: 'Uma letra maiúscula e uma minúscula', test: (value: string) => /[A-Z]/.test(value) && /[a-z]/.test(value) },
  { label: 'Um número', test: (value: string) => /\d/.test(value) },
  { label: 'Um símbolo (ex.: !, @, #)', test: (value: string) => /[^a-zA-Z0-9]/.test(value) },
]
const financialTips = [
  'Define um limite para despesas variáveis.',
  'Reserva primeiro uma pequena parte para poupar.',
  'Reve as tuas subscrições no início de cada mês.',
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
  const register = page === 'registo'
  const errors: Record<Field, string> = {
    name: values.name.trim().length < 2 ? 'Introduz o teu nome com pelo menos 2 caracteres.' : '',
    email: !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim()) ? 'Introduz um email válido, como nome@exemplo.pt.' : '',
    password: !values.password ? 'Introduz a tua password.' : register && !passwordRules.every(rule => rule.test(values.password)) ? 'A password deve cumprir todas as regras abaixo.' : '',
    confirm: values.confirm !== values.password || !values.confirm ? 'As passwords têm de ser iguais.' : '',
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
    const fields: Field[] = register ? ['name', 'email', 'password', 'confirm'] : ['email', 'password']
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
          value={values[key]} aria-invalid={invalid} aria-describedby={invalid ? `${key}-error` : key === 'password' && register ? 'password-rules' : undefined}
          onBlur={() => setTouched(previous => ({ ...previous, [key]: true }))}
          onChange={event => { setValues(previous => ({ ...previous, [key]: event.target.value })); setSuccess(false) }} />
        {key === 'password' && <button className="reveal" type="button" aria-controls="password" aria-pressed={showPassword} onClick={() => setShowPassword(!showPassword)}>{showPassword ? 'Ocultar' : 'Mostrar'}</button>}
      </div>
      {invalid && <p className="error" id={`${key}-error`}>{errors[key]}</p>}
    </div>
  }

  return <main className={`auth-layout auth-layout--${page}`}>
    <aside className="intro">
      <a className="brand" href="#login" onClick={event => { event.preventDefault(); navigate('login') }}><span className="brand-icon" aria-hidden="true">€</span> Personal Finance</a>
      <div className="intro-copy"><span className="eyebrow">AS TUAS FINANÇAS, COM CLAREZA</span><h1>Pequenos passos.<br />Mais controlo.</h1><p>Organiza as tuas despesas, planeia o mês e aproxima-te dos teus objetivos.</p><p className="financial-tip" aria-live="polite"><span aria-hidden="true">✦</span>{financialTips[tipIndex]}</p><div className="finance-teaser" aria-label="Exemplo de um resumo financeiro"><div className="balance-card"><span>Saldo disponível</span><strong>€2 480,00</strong><p><b aria-hidden="true">↗</b> +€248 este mês</p><svg className="balance-chart" viewBox="0 0 260 62" aria-hidden="true"><path d="M3 49C28 48 39 39 59 42S91 53 111 38s27-21 45-15 26 20 45 10 32-27 56-23" /><circle cx="3" cy="49" r="3" /><circle cx="111" cy="38" r="3" /><circle cx="201" cy="33" r="3" /><circle cx="257" cy="10" r="3" /></svg></div><div className="finance-card finance-card--budget"><span>Orçamento mensal</span><strong>72%</strong><i><b /></i></div><div className="finance-card finance-card--goal"><span>Meta de poupança</span><strong>4 / 6 meses</strong></div></div><div className="intro-detail"><span aria-hidden="true">↗</span> Um lugar para acompanhares o teu dinheiro.</div></div>
      <div className="intro-footer-spacer" aria-hidden="true" />
    </aside>
    <section className="form-panel" aria-labelledby="form-title">
      <div className="form-container">
        <nav className="page-switch" aria-label="Autenticação"><a href="#login" aria-current={!register ? 'page' : undefined} onClick={event => { event.preventDefault(); navigate('login') }}>Entrar</a><a href="#registo" aria-current={register ? 'page' : undefined} onClick={event => { event.preventDefault(); navigate('registo') }}>Criar conta</a></nav>
        <h2 id="form-title">{register ? 'Começa por aqui.' : 'Bem-vindo de volta.'}</h2>
        <p className="subtitle">{register ? 'Cria a tua conta e começa a organizar as tuas finanças.' : 'Entra na tua conta para acompanhar as tuas finanças.'}</p>
        <form noValidate onSubmit={submit}>
          {register && field('name', 'Nome', 'text', 'name', 'O teu nome')}
          {field('email', 'Email', 'email', 'email', 'nome@exemplo.pt')}
          {field('password', 'Password', showPassword ? 'text' : 'password', register ? 'new-password' : 'current-password', register ? 'Cria uma password' : 'A tua password')}
          {register && <ul className="rules" id="password-rules">{passwordRules.map(rule => <li key={rule.label} className={rule.test(values.password) ? 'met' : ''}><span aria-hidden="true">{rule.test(values.password) ? '✓' : '○'}</span> {rule.label}</li>)}</ul>}
          {register && field('confirm', 'Confirmar password', showPassword ? 'text' : 'password', 'new-password', 'Repete a password')}
          <button className="submit" type="submit">{register ? 'Criar conta' : 'Entrar'} <span aria-hidden="true">→</span></button>
          {success && <p className="success" role="status">Dados válidos! Este protótipo verifica o formulário; {register ? 'o registo' : 'o login'} ainda não está ligado à API.</p>}
        </form>
        <p className="alternate">{register ? 'Já tens conta?' : 'Ainda não tens conta?'} <a href={register ? '#login' : '#registo'} onClick={event => { event.preventDefault(); navigate(register ? 'login' : 'registo') }}>{register ? 'Entrar' : 'Criar conta'}</a></p>
        <p className="prototype">Protótipo de interface · Sem envio de dados</p>
      </div>
    </section>
  </main>
}

export default AuthPage

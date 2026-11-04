import { useState } from 'react'
import { passwordRules } from './validationRules'
type Field = 'name' | 'email' | 'password' | 'confirm'
const initial = { name: '', email: '', password: '', confirm: '' }

export function useAuthForm(register: boolean, email = '') {
  const [values, setValues] = useState({ ...initial, email })
  const [touched, setTouched] = useState<Partial<Record<Field, boolean>>>({})
  const [showPassword, setShowPassword] = useState(false)
  const [apiError, setApiError] = useState('')
  const [busy, setBusy] = useState(false)
  const errors: Record<Field, string> = {
    name: values.name.trim().length < 2 ? 'Introduz o teu nome com pelo menos 2 caracteres.' : '',
    email: !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim()) ? 'Introduz um email válido, como nome@exemplo.pt.' : '',
    password: !values.password ? 'Introduz a tua password.' : register && !passwordRules.every(rule => rule.test(values.password)) ? 'A password deve cumprir todas as regras abaixo.' : '',
    confirm: values.confirm !== values.password || !values.confirm ? 'As passwords têm de ser iguais.' : '',
  }

  function field(key: Field, label: string, type: string, autoComplete: string, placeholder: string) {
    const invalid = Boolean(touched[key] && errors[key])
    return <div className="field">
      <label htmlFor={key}>{label}</label>
      <div className={key === 'password' ? 'password-input' : undefined}>
        <input id={key} name={key} type={key === 'password' || key === 'confirm' ? showPassword ? 'text' : 'password' : type} autoComplete={autoComplete} placeholder={placeholder}
          required disabled={busy} maxLength={key === 'name' ? 150 : key === 'email' ? 256 : register ? 100 : undefined}
          value={values[key]} aria-invalid={invalid} aria-describedby={invalid ? `${key}-error` : key === 'password' && register ? 'password-rules' : undefined}
          onBlur={() => setTouched(previous => ({ ...previous, [key]: true }))}
          onChange={event => { setValues(previous => ({ ...previous, [key]: event.target.value })); setApiError('') }} />
        {key === 'password' && <button className="reveal" type="button" aria-controls="password" aria-pressed={showPassword} onClick={() => setShowPassword(!showPassword)}>{showPassword ? 'Hide' : 'Show'}</button>}
      </div>
      {invalid && <p className="error" id={`${key}-error`}>{errors[key]}</p>}
    </div>
  }

  function validate() {
    const fields: Field[] = register ? ['name', 'email', 'password', 'confirm'] : ['email', 'password']
    setTouched(Object.fromEntries(fields.map(field => [field, true])))
    const invalid = fields.find(field => errors[field])
    if (invalid) document.getElementById(invalid)?.focus()
    return !invalid
  }
  return { values, field, validate, busy, setBusy, apiError, setApiError }
}

import { passwordRules } from './validationRules'
export function PasswordRules({ password }: { password: string }) {
  return <ul className="rules" id="password-rules">{passwordRules.map(rule => <li key={rule.label} className={rule.test(password) ? 'met' : ''}>{rule.label}</li>)}</ul>
}

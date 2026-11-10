import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import '../../styles/auth.css'
export type AuthPageMode = 'login' | 'register' | 'forgot-password' | 'reset-password'
const financialTips = [
  'Set a limit for variable expenses.',
  'Set aside a small amount for savings first.',
  'Review your subscriptions at the beginning of each month.',
]


interface AuthPageProps {
  page: AuthPageMode
  onNavigate: (page: AuthPageMode) => void
  children: ReactNode
}
export default function AuthPage({ page, onNavigate: navigate, children }: AuthPageProps) {
  const [tipIndex, setTipIndex] = useState(0)
  useEffect(() => {
    const interval = window.setInterval(() => setTipIndex(current => (current + 1) % financialTips.length), 6000)
    return () => window.clearInterval(interval)
  }, [])
  return <main className={`auth-layout auth-layout--${page}`}>
    <aside className="intro">
      <a className="brand" href="#login" onClick={event => { event.preventDefault(); navigate('login') }}><span className="brand-icon" aria-hidden="true">€</span> Personal Finance</a>
      <div className="intro-copy"><span className="eyebrow">YOUR FINANCES, MADE CLEAR</span><h1>Small steps.<br />More control.</h1><p>Organize your expenses, plan your month, and get closer to your goals.</p><p className="financial-tip" aria-live="polite"><span aria-hidden="true">✦</span>{financialTips[tipIndex]}</p><div className="finance-teaser" aria-label="Example financial overview"><div className="balance-card"><span>Available balance</span><strong>€2,480.00</strong><p><b aria-hidden="true">↗</b> +€248 this month</p><svg className="balance-chart" viewBox="0 0 260 62" aria-hidden="true"><path d="M3 49C28 48 39 39 59 42S91 53 111 38s27-21 45-15 26 20 45 10 32-27 56-23" /><circle cx="3" cy="49" r="3" /><circle cx="111" cy="38" r="3" /><circle cx="201" cy="33" r="3" /><circle cx="257" cy="10" r="3" /></svg></div><div className="finance-card finance-card--budget"><span>Monthly budget</span><strong>72%</strong><i><b /></i></div><div className="finance-card finance-card--goal"><span>Savings goal</span><strong>4 / 6 months</strong></div></div><div className="intro-detail"><span aria-hidden="true">↗</span> One place to keep track of your money.</div></div>
      <div className="intro-footer-spacer" aria-hidden="true" />
    </aside>

    <section className="form-panel" aria-labelledby="form-title">
      <div className="form-container">{children}</div>
    </section>
  </main>
}

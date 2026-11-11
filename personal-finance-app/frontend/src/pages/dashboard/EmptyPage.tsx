import type { AuthPageMode } from '../auth/AuthPage'
import MainNavigation from './MainNavigation'
import type { User } from '../auth/auth'
import '../../styles/dashboard.css'

const titles: Record<'accounts' | 'summary' | 'budgets' | 'more', string> = {
  accounts: 'Accounts',
  summary: 'Summary',
  budgets: 'Budgets',
  more: 'More',
}

export default function EmptyPage({
  page,
  user,
  onNavigate,
  onLogout,
}: {
  page: 'accounts' | 'summary' | 'budgets' | 'more'
  user: User
  onNavigate: (page: AuthPageMode) => void
  onLogout: () => void
}) {
  return (
    <div className="dashboard empty-page">
      <header className="dashboard-header">
        <div>
          <span className="eyebrow">PERSONAL FINANCE</span>
          <h2 id="form-title">{titles[page]}</h2>
          <p className="subtitle">This area is ready for the next step.</p>
        </div>
        <button className="dashboard-logout" onClick={onLogout}>Sign out</button>
      </header>
      <MainNavigation page={page} onNavigate={onNavigate} />
      <p className="empty-page-message">Nothing here yet, {user.name}.</p>
    </div>
  )
}

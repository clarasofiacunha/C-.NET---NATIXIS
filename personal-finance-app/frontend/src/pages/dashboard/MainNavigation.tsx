import type { AuthPageMode } from '../auth/AuthPage'

const items: { page: AuthPageMode; label: string }[] = [
  { page: 'dashboard', label: 'Home' },
  { page: 'accounts', label: 'Accounts' },
  { page: 'summary', label: 'Summary' },
  { page: 'budgets', label: 'Budgets' },
  { page: 'more', label: 'More' },
]

export default function MainNavigation({
  page,
  onNavigate,
}: {
  page: AuthPageMode
  onNavigate: (page: AuthPageMode) => void
}) {
  return (
    <nav className="main-navigation" aria-label="Main navigation">
      {items.map(item => (
        <button
          className={page === item.page ? 'main-navigation__item main-navigation__item--active' : 'main-navigation__item'}
          key={item.page}
          type="button"
          aria-current={page === item.page ? 'page' : undefined}
          onClick={() => onNavigate(item.page)}
        >
          {item.label}
        </button>
      ))}
    </nav>
  )
}

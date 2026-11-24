import { useEffect, useMemo, useState } from 'react'
import { apiRequest } from '../../api/client'
import { authRequest } from '../auth/auth'
import type { User } from '../auth/auth'
import type { AuthPageMode } from '../auth/AuthPage'
import MainNavigation from './MainNavigation'
import '../../styles/dashboard.css'
import '../../styles/summary.css'

type Transaction = {
  id: string
  categoryName: string
  amount: number
  type: 'Income' | 'Expense'
  date: string
}

type CategoryExpense = {
  name: string
  amount: number
  percentage: number
  color: string
}

const money = new Intl.NumberFormat('en-IE', { style: 'currency', currency: 'EUR' })
const monthName = new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric' })
const categoryColors = ['#8f55b5', '#d77d62', '#4b9b83', '#e1b64d', '#5a82c7', '#c1689c', '#6d8c52']

function periodRange(year: number, month: number) {
  const lastDay = new Date(year, month, 0).getDate()
  const formattedMonth = String(month).padStart(2, '0')
  return {
    from: `${year}-${formattedMonth}-01`,
    to: `${year}-${formattedMonth}-${lastDay}`,
  }
}

function shiftMonth(period: { year: number; month: number }, direction: -1 | 1) {
  if (direction === -1) {
    return period.month === 1
      ? { year: period.year - 1, month: 12 }
      : { year: period.year, month: period.month - 1 }
  }
  return period.month === 12
    ? { year: period.year + 1, month: 1 }
    : { year: period.year, month: period.month + 1 }
}

export default function SummaryPage({
  user,
  onNavigate,
  onLogout,
}: {
  user: User
  onNavigate: (page: AuthPageMode) => void
  onLogout: () => void
}) {
  const today = new Date()
  const [period, setPeriod] = useState({ year: today.getFullYear(), month: today.getMonth() + 1 })
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [busy, setBusy] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    apiRequest<Transaction[]>('transactions', periodRange(period.year, period.month))
      .then(result => {
        if (active) setTransactions(result)
      })
      .catch(requestError => {
        if (active) setError(requestError instanceof Error ? requestError.message : 'Unable to load the summary.')
      })
      .finally(() => {
        if (active) setBusy(false)
      })
    return () => { active = false }
  }, [period.year, period.month])

  const expenses = useMemo(() => transactions.filter(transaction => transaction.type === 'Expense'), [transactions])
  const totalExpenses = useMemo(() => expenses.reduce((total, transaction) => total + transaction.amount, 0), [expenses])
  const categories = useMemo<CategoryExpense[]>(() => {
    const grouped = expenses.reduce<Map<string, number>>((totals, transaction) => {
      totals.set(transaction.categoryName, (totals.get(transaction.categoryName) ?? 0) + transaction.amount)
      return totals
    }, new Map())
    return [...grouped.entries()]
      .map(([name, amount], index) => ({
        name,
        amount,
        percentage: totalExpenses === 0 ? 0 : amount / totalExpenses * 100,
        color: categoryColors[index % categoryColors.length],
      }))
      .sort((first, second) => second.amount - first.amount)
  }, [expenses, totalExpenses])

  const periodDate = new Date(period.year, period.month - 1, 1)
  const isCurrentMonth = period.year === today.getFullYear() && period.month === today.getMonth() + 1
  const segments = useMemo(() => {
    return categories.reduce<{ offset: number; items: Array<CategoryExpense & { offset: number }> }>(
      (state, category) => ({
        offset: state.offset + category.percentage,
        items: [...state.items, { ...category, offset: state.offset }],
      }),
      { offset: 0, items: [] },
    ).items
  }, [categories])

  function changeMonth(direction: -1 | 1) {
    setBusy(true)
    setError('')
    setPeriod(current => shiftMonth(current, direction))
  }

  async function logout() {
    try {
      await authRequest<void>('logout')
    } finally {
      onLogout()
    }
  }

  return (
    <div className="dashboard summary-page">
      <header className="dashboard-header">
        <div>
          <span className="eyebrow">PERSONAL FINANCE</span>
          <h2 id="form-title">Monthly summary, {user.name}.</h2>
          <p className="subtitle">See where your money goes.</p>
        </div>
        <button className="dashboard-logout" onClick={logout}>Sign out</button>
      </header>
      <MainNavigation page="summary" onNavigate={onNavigate} />

      <section className="summary-period" aria-label="Select month">
        <button type="button" onClick={() => changeMonth(-1)} aria-label="Previous month">‹</button>
        <strong>{monthName.format(periodDate)}</strong>
        <button type="button" onClick={() => changeMonth(1)} aria-label="Next month" disabled={isCurrentMonth}>›</button>
      </section>

      {busy && <p role="status">Loading your summary...</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {!busy && !error && (
        <>
          <section className="summary-total" aria-label="Total monthly expenses">
            <span className="summary-total__icon" aria-hidden="true">€</span>
            <div>
              <span>Total expenses</span>
              <strong>{money.format(totalExpenses)}</strong>
              <small>{categories.length === 1 ? '1 category with expenses' : `${categories.length} categories with expenses`}</small>
            </div>
          </section>

          <section className="summary-breakdown" aria-labelledby="category-expenses-title">
            <div className="dashboard-section-title">
              <div>
                <h3 id="category-expenses-title">Expenses by category</h3>
                <p className="section-caption">Expense distribution in {monthName.format(periodDate)}</p>
              </div>
            </div>
            {categories.length > 0 ? (
              <div className="summary-chart-layout">
                <div className="donut-chart" role="img" aria-label={`Total expenses: ${money.format(totalExpenses)}`}>
                  <svg viewBox="0 0 42 42" aria-hidden="true">
                    {segments.map(category => (
                      <circle
                        key={category.name}
                        className="donut-chart__segment"
                        cx="21"
                        cy="21"
                        r="15.9155"
                        fill="transparent"
                        stroke={category.color}
                        strokeDasharray={`${category.percentage} ${100 - category.percentage}`}
                        strokeDashoffset={-category.offset}
                      />
                    ))}
                  </svg>
                  <div className="donut-chart__label"><strong>{money.format(totalExpenses)}</strong><span>total</span></div>
                </div>
                <ul className="category-list">
                  {categories.map(category => (
                    <li key={category.name}>
                      <i style={{ backgroundColor: category.color }} aria-hidden="true" />
                      <span className="category-list__name">{category.name}</span>
                      <strong>{category.percentage.toFixed(1)}%</strong>
                      <small>{money.format(category.amount)}</small>
                    </li>
                  ))}
                </ul>
              </div>
            ) : <p className="dashboard-empty">There are no expenses recorded this month.</p>}
          </section>
        </>
      )}
    </div>
  )
}

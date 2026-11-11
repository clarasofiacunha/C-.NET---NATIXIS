import { useEffect, useState } from 'react'
import { apiRequest } from '../../api/client'
import { authRequest } from '../auth/auth'
import type { User } from '../auth/auth'
import type { AuthPageMode } from '../auth/AuthPage'
import MainNavigation from './MainNavigation'
import '../../styles/dashboard.css'

type Summary = {
  totalBalance: number
  monthlyExpenses: number
  totalBudget: number
  budgetedExpenses: number
  budgetUsagePercentage: number
}

type Transaction = {
  id: string
  accountName: string
  categoryName: string
  amount: number
  type: 'Income' | 'Expense'
  date: string
  description?: string
}

const money = new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' })
const date = new Intl.DateTimeFormat('pt-PT', { day: '2-digit', month: 'short' })
const monthName = new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric' })
const chartMonthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export default function DashboardPage({
  user,
  onNavigate,
  onLogout,
}: {
  user: User
  onNavigate: (page: AuthPageMode) => void
  onLogout: () => void
}) {
  const [summary, setSummary] = useState<Summary | null>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [chartTransactions, setChartTransactions] = useState<Transaction[]>([])
  const currentDate = new Date()
  const [period, setPeriod] = useState({
    year: currentDate.getFullYear(),
    month: currentDate.getMonth() + 1,
  })
  const [busy, setBusy] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const firstChartMonth = new Date(period.year, period.month - 6, 1)
    const lastDay = new Date(period.year, period.month, 0).getDate()
    const periodStart = `${period.year}-${String(period.month).padStart(2, '0')}-01`
    const periodEnd = `${period.year}-${String(period.month).padStart(2, '0')}-${lastDay}`
    const chartStart = `${firstChartMonth.getFullYear()}-${String(firstChartMonth.getMonth() + 1).padStart(2, '0')}-01`
    Promise.all([
      apiRequest<Summary>('summary/monthly', period),
      apiRequest<Transaction[]>('transactions', {
        from: periodStart,
        to: periodEnd,
      }),
      apiRequest<Transaction[]>('transactions', { from: chartStart, to: periodEnd }),
    ])
      .then(([monthlySummary, userTransactions, historyTransactions]) => {
        if (userTransactions.length === 0 && historyTransactions.length > 0) {
          const latestTransaction = historyTransactions.reduce((latest, transaction) =>
            new Date(transaction.date) > new Date(latest.date) ? transaction : latest)
          const latestDate = new Date(latestTransaction.date)
          const latestPeriod = {
            year: latestDate.getUTCFullYear(),
            month: latestDate.getUTCMonth() + 1,
          }
          if (latestPeriod.year !== period.year || latestPeriod.month !== period.month) {
            setPeriod(latestPeriod)
            return
          }
        }
        setSummary(monthlySummary)
        setTransactions(userTransactions)
        setChartTransactions(historyTransactions)
      })
      .catch((requestError) => {
        setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar o dashboard.')
      })
      .finally(() => setBusy(false))
  }, [period])

  const selectedPeriodKey = `${period.year}-${period.month}`
  const chartMonths = Array.from({ length: 6 }, (_, index) => {
    const month = new Date(period.year, period.month - 6 + index, 1)
    const key = `${month.getFullYear()}-${month.getMonth() + 1}`
    const monthTransactions = chartTransactions.filter(transaction => {
      const transactionDate = new Date(transaction.date)
      return `${transactionDate.getUTCFullYear()}-${transactionDate.getUTCMonth() + 1}` === key
    })
    return {
      month,
      key,
      income: monthTransactions
        .filter(transaction => transaction.type === 'Income')
        .reduce((total, transaction) => total + transaction.amount, 0),
      expenses: monthTransactions
        .filter(transaction => transaction.type === 'Expense')
        .reduce((total, transaction) => total + transaction.amount, 0),
    }
  })
  const visibleChartMonths = chartMonths.filter(month => month.income > 0 || month.expenses > 0)
  const income = chartMonths.find(month => month.key === selectedPeriodKey)?.income ?? 0
  const expenses = chartMonths.find(month => month.key === selectedPeriodKey)?.expenses ?? 0
  const cashFlow = income - expenses
  const transactionsByDay = transactions.reduce<Record<string, Transaction[]>>((groups, transaction) => {
    const day = transaction.date.slice(0, 10)
    groups[day] = [...(groups[day] ?? []), transaction]
    return groups
  }, {})
  const chartMax = Math.max(...chartMonths.flatMap(month => [month.income, month.expenses]), 1)
  const periodDate = new Date(period.year, period.month - 1, 1)
  const now = new Date()
  const isCurrentOrFuture = period.year > now.getFullYear()
    || (period.year === now.getFullYear() && period.month >= now.getMonth() + 1)
  const changeMonth = (direction: -1 | 1) => {
    setBusy(true)
    setPeriod(current => direction === -1
      ? current.month === 1
        ? { year: current.year - 1, month: 12 }
        : { year: current.year, month: current.month - 1 }
      : current.month === 12
        ? { year: current.year + 1, month: 1 }
        : { year: current.year, month: current.month + 1 })
  }

  async function logout() {
    try {
      await authRequest<void>('logout')
    } finally {
      onLogout()
    }
  }

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div>
          <span className="eyebrow">PERSONAL FINANCE</span>
          <h2 id="form-title">Good morning, {user.name}.</h2>
          <p className="subtitle">Here is your financial overview.</p>
        </div>
        <button className="dashboard-logout" onClick={logout}>Sign out</button>
      </header>
      <MainNavigation page="dashboard" onNavigate={onNavigate} />
      {busy && <p role="status">Loading your overview...</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {!busy && !error && summary && (
        <>
          <section className="dashboard-stats" aria-label="Financial summary">
            <article className="dashboard-stat dashboard-stat--highlight">
              <span>Total balance</span>
              <strong>{money.format(summary.totalBalance)}</strong>
              <small>Across all accounts</small>
            </article>
            <article className="dashboard-stat">
              <span>{monthName.format(periodDate)} expenses</span>
              <strong>{money.format(summary.monthlyExpenses)}</strong>
              <small>{summary.budgetUsagePercentage.toFixed(0)}% of budget used</small>
            </article>
            <article className="dashboard-stat">
              <span>{monthName.format(periodDate)} budget</span>
              <strong>{money.format(summary.totalBudget)}</strong>
              <small>{money.format(summary.totalBudget - summary.budgetedExpenses)} remaining</small>
            </article>
          </section>
          <div className="dashboard-cash-flow">
            <span>{monthName.format(periodDate)} cash flow</span>
            <strong className={cashFlow >= 0 ? 'income' : 'expense'}>{cashFlow >= 0 ? '+' : ''}{money.format(cashFlow)}</strong>
          </div>
          <section className="dashboard-section dashboard-chart-section">
            <div className="dashboard-chart" aria-label="Income and expenses for the last six months">
              <div className="chart-bars">
                {visibleChartMonths.map(month => (
                  <button
                    className={month.key === selectedPeriodKey ? 'chart-month-group chart-month-group--selected' : 'chart-month-group'}
                    key={month.key}
                    type="button"
                    aria-label={`Select ${chartMonthNames[month.month.getMonth()]} ${month.month.getFullYear()}`}
                    onClick={() => setPeriod({ year: month.month.getFullYear(), month: month.month.getMonth() + 1 })}
                  >
                    <div className="chart-pair">
                      <div className="chart-bar chart-bar--income" style={{ height: `${Math.max((month.income / chartMax) * 100, month.income ? 4 : 0)}%` }} title={`Income: ${money.format(month.income)}`} />
                      <div className="chart-bar chart-bar--expense" style={{ height: `${Math.max((month.expenses / chartMax) * 100, month.expenses ? 4 : 0)}%` }} title={`Expenses: ${money.format(month.expenses)}`} />
                    </div>
                    <span className={month.key === selectedPeriodKey ? 'chart-month-label chart-month-label--current' : 'chart-month-label'}>
                      {chartMonthNames[month.month.getMonth()]} {month.month.getFullYear()}
                    </span>
                  </button>
                ))}
              </div>
              <div className="chart-selected-summary">
                <span><i className="chart-summary-dot chart-summary-dot--income">+</i><b>Income</b> {money.format(income)}</span>
                <span><i className="chart-summary-dot chart-summary-dot--expense">−</i><b>Expenses</b> {money.format(expenses)}</span>
              </div>
            </div>
            <div className="period-picker">
              <button type="button" onClick={() => changeMonth(-1)} aria-label="Previous month">‹</button>
              <strong>{monthName.format(periodDate)}</strong>
              <button type="button" onClick={() => changeMonth(1)} aria-label="Next month" disabled={isCurrentOrFuture}>›</button>
            </div>
          </section>
          <section className="dashboard-section">
            <div className="transaction-list">
              {transactions.length ? Object.entries(transactionsByDay).map(([day, dayTransactions]) => {
                const dayTotal = dayTransactions.reduce((total, transaction) => (
                  transaction.type === 'Income' ? total + transaction.amount : total - transaction.amount
                ), 0)
                return (
                  <div className="transaction-day" key={day}>
                    <div className="transaction-day-header">
                      <strong>{date.format(new Date(`${day}T12:00:00`))}</strong>
                      <strong className={dayTotal >= 0 ? 'income' : 'expense'}>{dayTotal >= 0 ? '+' : ''}{money.format(dayTotal)}</strong>
                    </div>
                    <div className="transaction-day-card">
                      {dayTransactions.map(transaction => (
                        <div className="transaction-row" key={transaction.id}>
                          <span className="transaction-icon">{transaction.type === 'Income' ? '↗' : '↘'}</span>
                          <div>
                            <strong>{transaction.categoryName}</strong>
                            <small>{transaction.description || 'No description'} · {transaction.accountName}</small>
                          </div>
                          <strong className={transaction.type === 'Income' ? 'income' : 'expense'}>{transaction.type === 'Income' ? '+' : '-'}{money.format(transaction.amount)}</strong>
                        </div>
                      ))}
                    </div>
                  </div>
                )
              }) : <p className="dashboard-empty">No transactions yet.</p>}
            </div>
          </section>
        </>
      )}
    </div>
  )
}

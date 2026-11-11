import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { authRequest, apiMutation, apiRequest } from '../auth/auth'
import type { User } from '../auth/auth'
import type { AuthPageMode } from '../auth/AuthPage'
import MainNavigation from './MainNavigation'

type Summary = {
  totalBalance: number
  monthlyExpenses: number
  totalBudget: number
  budgetedExpenses: number
  budgetUsagePercentage: number
}

type Transaction = {
  id: string
  accountId: string
  accountName: string
  categoryId: string
  categoryName: string
  amount: number
  type: 'Income' | 'Expense'
  date: string
  description?: string
}

type Account = { id: string; name: string }
type Category = { id: string; name: string; type: 'Income' | 'Expense' }

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
  const [accounts, setAccounts] = useState<Account[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null)
  const [transactionForm, setTransactionForm] = useState({
    accountId: '',
    categoryId: '',
    amount: '',
    type: 'Expense' as Transaction['type'],
    date: '',
    description: '',
  })
  const [transactionSaving, setTransactionSaving] = useState(false)
  const [transactionError, setTransactionError] = useState('')
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
      apiRequest<Account[]>('accounts'),
      apiRequest<Category[]>('categories'),
    ])
      .then(([monthlySummary, userTransactions, historyTransactions, loadedAccounts, loadedCategories]) => {
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
        setAccounts(loadedAccounts)
        setCategories(loadedCategories)
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

  function openEditTransaction(transaction: Transaction) {
      setTransactionError('')
      setEditingTransaction(transaction)
      setTransactionForm({
        accountId: transaction.accountId,
        categoryId: transaction.categoryId,
        amount: transaction.amount.toFixed(2).replace('.', ','),
        type: transaction.type,
        date: transaction.date.slice(0, 10),
        description: transaction.description ?? '',
      })
  }

  async function saveTransaction(event: FormEvent<HTMLFormElement>) {
      event.preventDefault()
      const amount = Number(transactionForm.amount.replace(',', '.'))
      if (!transactionForm.accountId || !transactionForm.categoryId || !transactionForm.date
        || !Number.isFinite(amount) || amount <= 0) {
        setTransactionError('Enter valid transaction details.')
        return
      }
      setTransactionSaving(true)
      setTransactionError('')
      try {
        await apiMutation<Transaction>(`transactions/${editingTransaction?.id}`, 'PUT', {
          accountId: transactionForm.accountId,
          categoryId: transactionForm.categoryId,
          amount,
          type: transactionForm.type,
          date: `${transactionForm.date}T12:00:00Z`,
          description: transactionForm.description,
        })
        setEditingTransaction(null)
        setBusy(true)
        window.location.hash = 'dashboard'
        setPeriod(current => ({ ...current }))
      } catch (requestError) {
        setTransactionError(requestError instanceof Error ? requestError.message : 'Unable to update the transaction.')
      } finally {
        setTransactionSaving(false)
      }
  }

  async function deleteTransaction(transaction: Transaction) {
      if (!window.confirm('Delete this transaction? This action cannot be undone.')) return
      setTransactionSaving(true)
      setError('')
      try {
        await apiMutation<void>(`transactions/${transaction.id}`, 'DELETE')
        setEditingTransaction(null)
        setBusy(true)
        setPeriod(current => ({ ...current }))
      } catch (requestError) {
        setError(requestError instanceof Error ? requestError.message : 'Unable to delete the transaction.')
      } finally {
        setTransactionSaving(false)
      }
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
                        <div
                          className="transaction-row"
                          key={transaction.id}
                          role="button"
                          tabIndex={0}
                          onClick={() => openEditTransaction(transaction)}
                          onKeyDown={event => {
                            if (event.key === 'Enter' || event.key === ' ') {
                              event.preventDefault()
                              openEditTransaction(transaction)
                            }
                          }}
                        >
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
      {editingTransaction && (
        <div className="account-modal" role="presentation">
          <div className="account-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="transaction-form-title">
            <div className="account-modal__header">
              <div>
                <h3 id="transaction-form-title">Edit transaction</h3>
                <p>Update the details of this transaction.</p>
              </div>
              <button type="button" className="account-modal__close" aria-label="Close form" onClick={() => setEditingTransaction(null)} disabled={transactionSaving}>×</button>
            </div>
            <form className="account-form transaction-form" onSubmit={saveTransaction}>
              <div className="account-form__fields">
                <div className="field">
                  <label htmlFor="transaction-type">Type</label>
                  <select id="transaction-type" value={transactionForm.type} onChange={event => setTransactionForm(form => ({ ...form, type: event.target.value as Transaction['type'] }))}>
                    <option value="Expense">Expense</option>
                    <option value="Income">Income</option>
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="transaction-amount">Amount</label>
                  <input id="transaction-amount" inputMode="decimal" value={transactionForm.amount} onChange={event => setTransactionForm(form => ({ ...form, amount: event.target.value }))} required />
                </div>
                <div className="field">
                  <label htmlFor="transaction-date">Date</label>
                  <input id="transaction-date" type="date" value={transactionForm.date} onChange={event => setTransactionForm(form => ({ ...form, date: event.target.value }))} required />
                </div>
                <div className="field">
                  <label htmlFor="transaction-account">Account</label>
                  <select id="transaction-account" value={transactionForm.accountId} onChange={event => setTransactionForm(form => ({ ...form, accountId: event.target.value }))}>
                    {accounts.map(account => <option key={account.id} value={account.id}>{account.name}</option>)}
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="transaction-category">Category</label>
                  <select id="transaction-category" value={transactionForm.categoryId} onChange={event => setTransactionForm(form => ({ ...form, categoryId: event.target.value }))}>
                    {categories.filter(category => category.type === transactionForm.type).map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="transaction-description">Description</label>
                  <input id="transaction-description" value={transactionForm.description} onChange={event => setTransactionForm(form => ({ ...form, description: event.target.value }))} />
                </div>
              </div>
              {transactionError && <p className="error" role="alert">{transactionError}</p>}
              <div className="account-modal__footer">
                <button
                  type="button"
                  className="transaction-delete-button"
                  onClick={() => void deleteTransaction(editingTransaction)}
                  disabled={transactionSaving}
                  aria-label="Delete transaction"
                  title="Delete transaction"
                >
                  <ActionIcon />
                  <span>Delete transaction</span>
                </button>
                <button type="button" className="account-modal__cancel" onClick={() => setEditingTransaction(null)} disabled={transactionSaving}>Cancel</button>
                <button className="submit account-form__submit" type="submit" disabled={transactionSaving}>{transactionSaving ? 'Saving...' : 'Save transaction'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

function ActionIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M5 7h14M10 4h4l1 3H9l1-3ZM8 10v7m4-7v7m4-7v7M6 7l1 13h10l1-13" />
    </svg>
  )
}

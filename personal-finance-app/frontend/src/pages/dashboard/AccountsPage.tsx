import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { apiMutation, apiRequest } from '../../api/client'
import { authRequest } from '../auth/auth'
import type { User } from '../auth/auth'
import type { AuthPageMode } from '../auth/AuthPage'
import MainNavigation from './MainNavigation'
import '../../styles/accounts.css'
import '../../styles/dashboard.css'

type AccountType = 'Checking' | 'Savings' | 'Cash' | 'CreditCard' | 'Investment' | 'Loan' | 'Other'

type Account = {
  id: string
  name: string
  type: AccountType
  initialBalance: number
  balance: number
  createdAt: string
}

type Transaction = {
  accountId: string
  amount: number
  type: 'Income' | 'Expense'
  date: string
}

type ChartPeriod = '7D' | '1M' | '1Y' | 'MAX'

const money = new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' })
const chartMoney = new Intl.NumberFormat('en-GB', {
  style: 'currency',
  currency: 'EUR',
  maximumFractionDigits: 0,
})
const typeLabels: Record<AccountType, string> = {
  Checking: 'Checking',
  Savings: 'Savings',
  Cash: 'Cash',
  CreditCard: 'Credit card',
  Investment: 'Investment',
  Loan: 'Loan',
  Other: 'Other',
}

function pad(value: number) {
  return String(value).padStart(2, '0')
}

function formatChartDate(date: Date, period: ChartPeriod) {
  if (period === '7D' || period === '1M') {
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`
  }
  if (period === '1Y') {
    return `${pad(date.getMonth() + 1)}/${String(date.getFullYear()).slice(-2)}`
  }
  return `${new Intl.DateTimeFormat('en-GB', { month: 'short' }).format(date).toLowerCase()}'${String(date.getFullYear()).slice(-2)}`
}

function startOfDay(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

function addDays(date: Date, days: number) {
  const result = new Date(date)
  result.setDate(result.getDate() + days)
  return result
}

function AccountIcon({ type }: { type: AccountType }) {
  return (
    <span className={`account-icon account-icon--${type.toLowerCase()}`} aria-hidden="true">
      {type === 'Savings' ? '⌁'
        : type === 'Cash' ? '€'
          : type === 'CreditCard' ? '▣'
            : type === 'Investment' ? '↗'
              : type === 'Loan' ? '−'
                : type === 'Other' ? '•'
                  : '↗'}
    </span>
  )
}

function ActionIcon({ action }: { action: 'edit' | 'delete' }) {
  return action === 'edit' ? (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="m4 16.5-.8 3.8 3.8-.8L18.6 7.9a2 2 0 0 0-2.8-2.8L4 16.5Z" />
      <path d="m14.4 6.4 3.2 3.2" />
    </svg>
  ) : (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M5 7h14M10 4h4l1 3H9l1-3ZM8 10v7m4-7v7m4-7v7M6 7l1 13h10l1-13" />
    </svg>
  )
}

export default function AccountsPage({
  user,
  onNavigate,
  onLogout,
}: {
  user: User
  onNavigate: (page: AuthPageMode) => void
  onLogout: () => void
}) {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [busy, setBusy] = useState(true)
  const [saving, setSaving] = useState(false)
  const [chartPeriod, setChartPeriod] = useState<ChartPeriod>('MAX')
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingAccount, setEditingAccount] = useState<Account | null>(null)
  const [error, setError] = useState('')
  const [notification, setNotification] = useState('')
  const [formError, setFormError] = useState('')
  const [form, setForm] = useState({ name: '', type: 'Checking' as AccountType, initialBalance: '' })
  const notificationTimeout = useRef<number | undefined>(undefined)

  const loadAccounts = useCallback(() => {
    return Promise.all([
      apiRequest<Account[]>('accounts'),
      apiRequest<Transaction[]>('transactions'),
    ])
      .then(([loadedAccounts, loadedTransactions]) => {
        setAccounts(loadedAccounts)
        setTransactions(loadedTransactions)
      })
      .catch(requestError => {
        setError(requestError instanceof Error ? requestError.message : 'Unable to load your accounts.')
      })
      .finally(() => setBusy(false))
  }, [])

  useEffect(() => {
    void loadAccounts()
  }, [loadAccounts])

  useEffect(() => () => {
    if (notificationTimeout.current !== undefined) {
      window.clearTimeout(notificationTimeout.current)
    }
  }, [])

  const totalBalance = accounts.reduce((total, account) => total + account.balance, 0)
  const chartData = useMemo(() => {
    if (accounts.length === 0) return []
    const orderedTransactions = [...transactions].sort((a, b) => (
      new Date(a.date).getTime() - new Date(b.date).getTime()
    ))
    const today = startOfDay(new Date())
    const accountStart = new Map(accounts.map(account => {
      const firstTransaction = orderedTransactions
        .filter(transaction => transaction.accountId === account.id)
        .map(transaction => new Date(transaction.date))
        .sort((a, b) => a.getTime() - b.getTime())[0]
      const createdAt = new Date(account.createdAt)
      return [account.id, firstTransaction && firstTransaction < createdAt ? firstTransaction : createdAt] as const
    }))
    const firstStart = accounts.reduce<Date | null>((first, account) => {
      const start = accountStart.get(account.id) ?? today
      return first === null || start < first ? start : first
    }, null) ?? today
    const firstMonth = new Date(firstStart.getFullYear(), firstStart.getMonth(), 1)
    const start = chartPeriod === '7D'
      ? addDays(today, -6)
      : chartPeriod === '1M'
        ? addDays(today, -29)
        : chartPeriod === '1Y'
          ? new Date(today.getFullYear(), today.getMonth() - 11, 1)
          : firstMonth
    const points: { date: Date; value: number }[] = []
    const monthly = chartPeriod === '1Y' || chartPeriod === 'MAX'
    for (let cursor = new Date(start); cursor <= today; cursor = monthly
      ? new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1)
      : addDays(cursor, 1)) {
      const bucketStart = new Date(cursor)
      const bucketEnd = monthly
        ? cursor.getFullYear() === today.getFullYear() && cursor.getMonth() === today.getMonth()
          ? addDays(today, 1)
          : new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1)
        : addDays(cursor, 1)
      const value = accounts
        .filter(account => (accountStart.get(account.id) ?? today) < bucketEnd)
        .reduce((total, account) => total + account.initialBalance, 0)
        + orderedTransactions
          .filter(transaction => new Date(transaction.date) < bucketEnd)
          .reduce((total, transaction) => total + (transaction.type === 'Income' ? transaction.amount : -transaction.amount), 0)
      points.push({ date: bucketStart, value })
    }
    return points.length > 1 ? points : [{ date: today, value: totalBalance }]
  }, [accounts, transactions, totalBalance, chartPeriod])

  const chart = useMemo(() => {
    if (chartData.length === 0) return null
    const width = 620
    const height = 220
    const padding = { top: 24, right: 18, bottom: 42, left: 48 }
    const values = chartData.map(item => item.value)
    const minimum = Math.min(...values)
    const maximum = Math.max(...values)
    const rawStep = Math.max((maximum - minimum) / 2, 10)
    const magnitude = 10 ** Math.floor(Math.log10(rawStep))
    const normalizedStep = rawStep / magnitude
    const step = Math.max(10, (normalizedStep <= 1 ? 1 : normalizedStep <= 2 ? 2 : normalizedStep <= 5 ? 5 : 10) * magnitude)
    const axisMinimum = Math.floor(minimum / step) * step
    const axisMaximum = Math.max(axisMinimum + step * 2, Math.ceil(maximum / step) * step)
    const range = axisMaximum - axisMinimum
    const points = chartData.map((item, index) => {
      const x = padding.left + (index / Math.max(chartData.length - 1, 1)) * (width - padding.left - padding.right)
      const y = padding.top + (1 - (item.value - axisMinimum) / range) * (height - padding.top - padding.bottom)
      return { ...item, x, y }
    })
    return {
      points,
      line: points.map(point => `${point.x},${point.y}`).join(' '),
      area: `${points[0].x},${height - padding.bottom} ${points.map(point => `${point.x},${point.y}`).join(' ')} ${points.at(-1)?.x},${height - padding.bottom}`,
      yTicks: [
        { value: axisMaximum, y: padding.top },
        { value: axisMinimum + range / 2, y: height / 2 - 1 },
        { value: axisMinimum, y: height - padding.bottom },
      ],
    }
  }, [chartData])

  function chartLabel(date: Date, index: number) {
    if (chartPeriod === '7D') {
      return formatChartDate(date, chartPeriod)
    }
    const labelCount = 6
    const labelIndexes = new Set(
      Array.from({ length: labelCount }, (_, labelIndex) => (
        Math.round(labelIndex * (chartData.length - 1) / (labelCount - 1))
      )),
    )
    return labelIndexes.has(index) ? formatChartDate(date, chartPeriod) : ''
  }

  const chartDescription: Record<ChartPeriod, string> = {
    '7D': 'Last 7 days',
    '1M': 'Last month',
    '1Y': 'Last year',
    MAX: 'All history',
  }

  function updateForm(field: 'name' | 'type' | 'initialBalance', value: string) {
    setForm(current => ({ ...current, [field]: value }))
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFormError('')
    const initialBalance = Number(form.initialBalance.replace(',', '.'))
    if (!form.name.trim() || !Number.isFinite(initialBalance)) {
      setFormError('Enter an account name and a valid opening balance.')
      return
    }
    setSaving(true)
    try {
      await apiMutation<Account>(
        editingAccount ? `accounts/${editingAccount.id}` : 'accounts',
        editingAccount ? 'PUT' : 'POST',
        {
        name: form.name.trim(),
        type: form.type,
        initialBalance,
        },
      )
      setForm({ name: '', type: 'Checking', initialBalance: '' })
      setIsFormOpen(false)
      setEditingAccount(null)
      setError('')
      setBusy(true)
      await loadAccounts()
    } catch (requestError) {
      setFormError(requestError instanceof Error
        ? requestError.message
        : editingAccount ? 'Unable to update the account.' : 'Unable to add the account.')
    } finally {
      setSaving(false)
    }
  }

  function openCreateForm() {
    setForm({ name: '', type: 'Checking', initialBalance: '' })
    setFormError('')
    setEditingAccount(null)
    setIsFormOpen(true)
  }

  function openEditForm(account: Account) {
    setForm({
      name: account.name,
      type: account.type,
      initialBalance: account.initialBalance.toFixed(2).replace('.', ','),
    })
    setFormError('')
    setEditingAccount(account)
    setIsFormOpen(true)
  }

  async function deleteAccount(account: Account) {
    if (!window.confirm(`Delete "${account.name}"? This action cannot be undone.`)) return
    setError('')
    setSaving(true)
    try {
      await apiMutation<void>(`accounts/${account.id}`, 'DELETE')
      setBusy(true)
      await loadAccounts()
    } catch (requestError) {
      setNotification(requestError instanceof Error ? requestError.message : 'Unable to delete the account.')
      if (notificationTimeout.current !== undefined) {
        window.clearTimeout(notificationTimeout.current)
      }
      notificationTimeout.current = window.setTimeout(() => {
        setNotification('')
        notificationTimeout.current = undefined
      }, 5000)
    } finally {
      setSaving(false)
    }
  }

  async function logout() {
    try {
      await authRequest<void>('logout')
    } finally {
      onLogout()
    }
  }

  const isEditing = editingAccount !== null

  return (
    <div className="dashboard accounts-page">
      <header className="dashboard-header">
        <div>
          <span className="eyebrow">PERSONAL FINANCE</span>
          <h2 id="form-title">Your accounts, {user.name}.</h2>
          <p className="subtitle">Keep a clear view of where your money is.</p>
        </div>
        <button className="dashboard-logout" onClick={logout}>Sign out</button>
      </header>
      <MainNavigation page="accounts" onNavigate={onNavigate} />
      {busy && <p role="status">Loading your accounts...</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {notification && <p className="account-notification" role="alert">{notification}</p>}
      {!busy && !error && (
        <>
          <section className="accounts-hero" aria-label="Total balance">
            <div>
              <span className="accounts-hero__label">Total balance</span>
              <strong>{money.format(totalBalance)}</strong>
              <small>Across all active accounts</small>
            </div>
            <span className="accounts-hero__mark" aria-hidden="true">€</span>
          </section>

          <section className="dashboard-section">
            <div className="dashboard-section-title">
              <h3>My accounts <span className="section-count">{accounts.length}</span></h3>
              <button type="button" onClick={openCreateForm}>+ Add account</button>
            </div>
            {accounts.length > 0 ? (
              <div className="accounts-list">
                {accounts.map(account => (
                  <article className="account-card" key={account.id}>
                    <AccountIcon type={account.type} />
                    <div className="account-card__details">
                      <strong>{account.name}</strong>
                      <span>{typeLabels[account.type]}</span>
                    </div>
                    <strong className={account.balance < 0 ? 'expense' : 'account-card__balance'}>{money.format(account.balance)}</strong>
                    <div className="account-card__actions">
                      <button type="button" className="account-action account-action--edit" aria-label={`Edit ${account.name}`} title={`Edit ${account.name}`} onClick={() => openEditForm(account)}><ActionIcon action="edit" /></button>
                      <button type="button" className="account-action account-action--delete" aria-label={`Delete ${account.name}`} title={`Delete ${account.name}`} onClick={() => void deleteAccount(account)} disabled={saving}><ActionIcon action="delete" /></button>
                    </div>
                  </article>
                ))}
              </div>
            ) : <p className="dashboard-empty">You do not have any accounts yet. Add your first account above.</p>}
          </section>

          <section className="dashboard-section accounts-history">
            <div className="dashboard-section-title">
              <div>
                <h3>Balance history</h3>
                <p className="section-caption">{chartDescription[chartPeriod]}</p>
              </div>
            </div>
            {chart ? (
              <div className="balance-history">
                <svg viewBox="0 0 620 220" role="img" aria-label="Total balance history over time">
                  <defs>
                    <linearGradient id="balance-area" x1="0" x2="0" y1="0" y2="1">
                      <stop offset="0%" stopColor="#b27bca" stopOpacity=".25" />
                      <stop offset="100%" stopColor="#b27bca" stopOpacity="0" />
                    </linearGradient>
                  </defs>
                  {chart.yTicks.map(tick => (
                    <g key={tick.y}>
                      <line className="chart-grid-line" x1="48" x2="602" y1={tick.y} y2={tick.y} />
                      <text className="balance-history__y-label" x="4" y={tick.y + 4}>{chartMoney.format(tick.value)}</text>
                    </g>
                  ))}
                  <polygon className="balance-history__area" points={chart.area} />
                  <polyline className="balance-history__line" points={chart.line} />
                  {chart.points.map((point, index) => (
                    chartLabel(point.date, index) && <text key={`${point.date.toISOString()}-${index}`} className="balance-history__label" x={point.x} y="207" textAnchor="middle">{chartLabel(point.date, index)}</text>
                  ))}
                </svg>
                <div className="chart-periods" aria-label="Chart time period">
                  {(['7D', '1M', '1Y', 'MAX'] as ChartPeriod[]).map(period => (
                    <button
                      className={chartPeriod === period ? 'chart-period chart-period--active' : 'chart-period'}
                      key={period}
                      type="button"
                      aria-pressed={chartPeriod === period}
                      onClick={() => setChartPeriod(period)}
                    >
                      {period}
                    </button>
                  ))}
                </div>
              </div>
            ) : <p className="dashboard-empty">There is not enough history to display yet.</p>}
          </section>
        </>
      )}
      {isFormOpen && (
        <div className="account-modal" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget && !saving) { setIsFormOpen(false); setEditingAccount(null) } }}>
          <div className="account-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="account-form-title">
            <div className="account-modal__header">
              <div>
                <h3 id="account-form-title">{isEditing ? 'Edit account' : 'New account'}</h3>
                <p>{isEditing ? 'Update this account details.' : 'Add an account to track its balance.'}</p>
              </div>
              <button type="button" className="account-modal__close" aria-label="Fechar formulário" onClick={() => { setIsFormOpen(false); setEditingAccount(null) }} disabled={saving}>×</button>
            </div>
            <form className="account-form" onSubmit={submit}>
              <div className="account-form__fields">
                <div className="field">
                  <label htmlFor="account-name">Account name</label>
                  <input id="account-name" value={form.name} onChange={event => updateForm('name', event.target.value)} placeholder="e.g. Main account" required />
                </div>
                <div className="field">
                  <label htmlFor="account-type">Type</label>
                  <select id="account-type" value={form.type} onChange={event => updateForm('type', event.target.value)}>
                    {Object.entries(typeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="account-balance">Opening balance</label>
                  <input id="account-balance" inputMode="decimal" value={form.initialBalance} onChange={event => updateForm('initialBalance', event.target.value)} placeholder="0.00" required />
                </div>
              </div>
              {formError && <p className="error" role="alert">{formError}</p>}
              <div className="account-modal__footer">
                <button type="button" className="account-modal__cancel" onClick={() => { setIsFormOpen(false); setEditingAccount(null) }} disabled={saving}>Cancel</button>
                <button className="submit account-form__submit" type="submit" disabled={saving}>{saving ? 'Saving...' : 'Save account'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

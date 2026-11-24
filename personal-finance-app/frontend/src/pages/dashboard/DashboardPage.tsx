import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { apiMutation, apiRequest, apiRequestAllPages } from '../../api/client'
import type { PagedResponse } from '../../api/client'
import { authRequest } from '../auth/auth'
import type { User } from '../auth/auth'
import type { AuthPageMode } from '../auth/AuthPage'
import MainNavigation from './MainNavigation'
import { groupAccountsByType } from './accountTypes'
import ConfirmDialog from './ConfirmDialog'
import type { AccountType } from './accountTypes'
import '../../styles/dashboard.css'
import '../../styles/accounts.css'

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

type Account = { id: string; name: string; type: AccountType }
type Category = { id: string; name: string; type: 'Income' | 'Expense' }

const money = new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' })
const date = new Intl.DateTimeFormat('pt-PT', { day: '2-digit', month: 'short' })
const monthName = new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric' })
const chartMonthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

const PAGE_SIZE = 20
const sortOptions = {
  'date-desc': { label: 'Most recent', sortBy: 'Date', sortDirection: 'Desc' },
  'date-asc': { label: 'Oldest', sortBy: 'Date', sortDirection: 'Asc' },
  'amount-desc': { label: 'Highest amount', sortBy: 'Amount', sortDirection: 'Desc' },
  'amount-asc': { label: 'Lowest amount', sortBy: 'Amount', sortDirection: 'Asc' },
  'description-asc': { label: 'Description A–Z', sortBy: 'Description', sortDirection: 'Asc' },
} as const
type SortKey = keyof typeof sortOptions
const emptyFilters = { categoryId: '', accountId: '', type: '', minAmount: '', maxAmount: '' }
type ListResult = { key: string; data?: PagedResponse<Transaction>; error?: string }

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
  const [filters, setFilters] = useState(emptyFilters)
  const [sort, setSort] = useState<SortKey>('date-desc')
  const [page, setPage] = useState(1)
  const [listResult, setListResult] = useState<ListResult | null>(null)
  const [chartTransactions, setChartTransactions] = useState<Transaction[]>([])
  const [accounts, setAccounts] = useState<Account[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null)
  const [transactionFormOpen, setTransactionFormOpen] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
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
  const [addingCategory, setAddingCategory] = useState(false)
  const [categoryName, setCategoryName] = useState('')
  const [categoryType, setCategoryType] = useState<Category['type']>('Expense')
  const [categorySaving, setCategorySaving] = useState(false)
  const [categoryError, setCategoryError] = useState('')
  const currentDate = new Date()
  const [period, setPeriod] = useState({
    year: currentDate.getFullYear(),
    month: currentDate.getMonth() + 1,
  })
  const [busy, setBusy] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const firstChartMonth = new Date(period.year, period.month - 6, 1)
    const { end: periodEnd } = periodRange(period)
    const chartStart = `${firstChartMonth.getFullYear()}-${String(firstChartMonth.getMonth() + 1).padStart(2, '0')}-01`
    Promise.all([
      apiRequest<Summary>('summary/monthly', period),
      apiRequestAllPages<Transaction>('transactions', { from: chartStart, to: periodEnd }),
      apiRequest<Account[]>('accounts'),
      apiRequest<Category[]>('categories'),
    ])
      .then(([monthlySummary, historyTransactions, loadedAccounts, loadedCategories]) => {
        const hasPeriodTransactions = historyTransactions.some(transaction => {
          const transactionDate = new Date(transaction.date)
          return transactionDate.getUTCFullYear() === period.year && transactionDate.getUTCMonth() + 1 === period.month
        })
        if (!hasPeriodTransactions && historyTransactions.length > 0) {
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
        setChartTransactions(historyTransactions)
        setAccounts(loadedAccounts)
        setCategories(loadedCategories)
      })
      .catch((requestError) => {
        setError(requestError instanceof Error ? requestError.message : 'Unable to load your overview.')
      })
      .finally(() => setBusy(false))
  }, [period])

  // Lista do mes: paginada, filtrada e ordenada no servidor.
  const { start: periodStart, end: periodEnd } = periodRange(period)
  const listQuery: Record<string, string | number> = {
    from: periodStart,
    to: periodEnd,
    sortBy: sortOptions[sort].sortBy,
    sortDirection: sortOptions[sort].sortDirection,
    page,
    pageSize: PAGE_SIZE,
  }
  if (filters.categoryId) listQuery.categoryId = filters.categoryId
  if (filters.accountId) listQuery.accountId = filters.accountId
  if (filters.type) listQuery.type = filters.type
  if (filters.minAmount.trim()) listQuery.minAmount = filters.minAmount.trim().replace(',', '.')
  if (filters.maxAmount.trim()) listQuery.maxAmount = filters.maxAmount.trim().replace(',', '.')
  const listKey = JSON.stringify(listQuery)

  useEffect(() => {
    let active = true
    const query = JSON.parse(listKey) as Record<string, string | number>
    // Pequena espera para nao pedir a cada tecla nos campos de montante.
    const timer = window.setTimeout(() => {
      apiRequest<PagedResponse<Transaction>>('transactions', query)
        .then(data => {
          if (!active) return
          if (data.items.length === 0 && data.page > 1 && data.totalPages > 0) {
            setPage(data.totalPages)
            return
          }
          setListResult({ key: listKey, data })
        })
        .catch(requestError => {
          if (active) setListResult({
            key: listKey,
            error: requestError instanceof Error ? requestError.message : 'Unable to load transactions.',
          })
        })
    }, 250)
    return () => {
      active = false
      window.clearTimeout(timer)
    }
  }, [listKey, period])

  const listLoading = listResult?.key !== listKey
  const transactions = listResult?.data?.items ?? []
  const totalPages = listResult?.data?.totalPages ?? 0
  const totalCount = listResult?.data?.totalCount ?? 0
  const hasFilters = Object.values(filters).some(Boolean)
  const accountGroups = groupAccountsByType(accounts)
  const updateFilter = (name: keyof typeof emptyFilters, value: string) => {
    setFilters(current => ({ ...current, [name]: value }))
    setPage(1)
  }

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
  // Agrupa transacoes consecutivas do mesmo dia (mantem a ordem escolhida).
  const transactionsByDay = transactions.reduce<{ day: string; items: Transaction[] }[]>((groups, transaction) => {
    const day = transaction.date.slice(0, 10)
    const last = groups[groups.length - 1]
    if (last?.day === day) last.items.push(transaction)
    else groups.push({ day, items: [transaction] })
    return groups
  }, [])
  const chartMax = Math.max(...chartMonths.flatMap(month => [month.income, month.expenses]), 1)
  const periodDate = new Date(period.year, period.month - 1, 1)
  const now = new Date()
  const isCurrentOrFuture = period.year > now.getFullYear()
    || (period.year === now.getFullYear() && period.month >= now.getMonth() + 1)
  const changeMonth = (direction: -1 | 1) => {
    setBusy(true)
    setPage(1)
    setPeriod(current => direction === -1
      ? current.month === 1
        ? { year: current.year - 1, month: 12 }
        : { year: current.year, month: current.month - 1 }
      : current.month === 12
        ? { year: current.year + 1, month: 1 }
        : { year: current.year, month: current.month + 1 })
  }

  // Primeira categoria do tipo indicado (mantem a categoria atual se ainda for valida).
  function categoryFor(type: Transaction['type'], currentId = '') {
    const options = categories.filter(category => category.type === type)
    return options.some(category => category.id === currentId) ? currentId : options[0]?.id ?? ''
  }

  function closeTransactionForm() {
    setTransactionFormOpen(false)
    setEditingTransaction(null)
    setAddingCategory(false)
  }

  // O formulario de criacao e o mesmo da edicao, comecando com valores por omissao.
  function openCreateTransaction() {
    const today = new Date()
    setTransactionError('')
    setAddingCategory(false)
    setEditingTransaction(null)
    setTransactionForm({
      accountId: groupAccountsByType(accounts)[0]?.accounts[0]?.id ?? '',
      categoryId: categoryFor('Expense'),
      amount: '',
      type: 'Expense',
      date: `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`,
      description: '',
    })
    setTransactionFormOpen(true)
  }

  function openEditTransaction(transaction: Transaction) {
      setTransactionError('')
      setAddingCategory(false)
      setEditingTransaction(transaction)
      setTransactionFormOpen(true)
      setTransactionForm({
        accountId: transaction.accountId,
        categoryId: transaction.categoryId,
        amount: transaction.amount.toFixed(2).replace('.', ','),
        type: transaction.type,
        date: transaction.date.slice(0, 10),
        description: transaction.description ?? '',
      })
  }

  function openAddCategory() {
    setCategoryName('')
    setCategoryType(transactionForm.type)
    setCategoryError('')
    setAddingCategory(true)
  }

  async function saveCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (categorySaving) return
    const name = categoryName.trim()
    if (!name || name.length > 100) {
      setCategoryError('Enter a category name between 1 and 100 characters.')
      return
    }
    setCategorySaving(true)
    setCategoryError('')
    try {
      const category = await apiMutation<Category>('categories', 'POST', {
        name,
        type: categoryType,
      })
      setCategories(current => [...current.filter(item => item.id !== category.id), category]
        .sort((a, b) => a.name.localeCompare(b.name)))
      setTransactionForm(form => category.type === form.type
        ? { ...form, categoryId: category.id }
        : form)
      setAddingCategory(false)
    } catch (requestError) {
      setCategoryError(requestError instanceof Error ? requestError.message : 'Unable to create the category.')
    } finally {
      setCategorySaving(false)
    }
  }

  async function saveTransaction(event: FormEvent<HTMLFormElement>) {
      event.preventDefault()
      const amount = Number(transactionForm.amount.replace(',', '.'))
      if (!transactionForm.accountId || !categories.some(category => category.id === transactionForm.categoryId && category.type === transactionForm.type) || !transactionForm.date
        || !Number.isFinite(amount) || amount <= 0) {
        setTransactionError('Enter valid transaction details.')
        return
      }
      setTransactionSaving(true)
      setTransactionError('')
      const body = {
        accountId: transactionForm.accountId,
        categoryId: transactionForm.categoryId,
        amount,
        type: transactionForm.type,
        date: `${transactionForm.date}T12:00:00Z`,
        description: transactionForm.description,
      }
      try {
        if (editingTransaction) {
          await apiMutation<Transaction>(`transactions/${editingTransaction.id}`, 'PUT', body)
          setPeriod(current => ({ ...current }))
        } else {
          await apiMutation<Transaction>('transactions', 'POST', body)
          // Mostra o mes da nova transacao, a partir da primeira pagina.
          const [year, month] = transactionForm.date.split('-').map(Number)
          setPage(1)
          setPeriod({ year, month })
        }
        closeTransactionForm()
        setBusy(true)
        window.location.hash = 'dashboard'
      } catch (requestError) {
        setTransactionError(requestError instanceof Error
          ? requestError.message
          : editingTransaction ? 'Unable to update the transaction.' : 'Unable to create the transaction.')
      } finally {
        setTransactionSaving(false)
      }
  }

  async function deleteTransaction(transaction: Transaction) {
      setTransactionSaving(true)
      setTransactionError('')
      try {
        await apiMutation<void>(`transactions/${transaction.id}`, 'DELETE')
        setConfirmingDelete(false)
        closeTransactionForm()
        setBusy(true)
        setPeriod(current => ({ ...current }))
      } catch (requestError) {
        // Mostra o erro no formulario (que continua aberto) em vez de esconder o dashboard.
        setConfirmingDelete(false)
        setTransactionError(requestError instanceof Error ? requestError.message : 'Unable to delete the transaction.')
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
    <div className="dashboard dashboard--with-fab">
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
                    onClick={() => {
                      setPage(1)
                      setPeriod({ year: month.month.getFullYear(), month: month.month.getMonth() + 1 })
                    }}
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
            <div className="dashboard-section-title">
              <h3>Transactions</h3>
            </div>
            <div className="transaction-filters" role="search" aria-label="Filter and sort transactions">
              <label>
                <span>Category</span>
                <select value={filters.categoryId} onChange={event => updateFilter('categoryId', event.target.value)}>
                  <option value="">All</option>
                  {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
                </select>
              </label>
              <label>
                <span>Account</span>
                <select value={filters.accountId} onChange={event => updateFilter('accountId', event.target.value)}>
                  <option value="">All</option>
                  {accounts.map(account => <option key={account.id} value={account.id}>{account.name}</option>)}
                </select>
              </label>
              <label>
                <span>Type</span>
                <select value={filters.type} onChange={event => updateFilter('type', event.target.value)}>
                  <option value="">All</option>
                  <option value="Expense">Expense</option>
                  <option value="Income">Income</option>
                </select>
              </label>
              <label>
                <span>Min €</span>
                <input inputMode="decimal" placeholder="0,00" value={filters.minAmount} onChange={event => updateFilter('minAmount', event.target.value)} />
              </label>
              <label>
                <span>Max €</span>
                <input inputMode="decimal" placeholder="Any" value={filters.maxAmount} onChange={event => updateFilter('maxAmount', event.target.value)} />
              </label>
              <label>
                <span>Sort by</span>
                <select value={sort} onChange={event => { setSort(event.target.value as SortKey); setPage(1) }}>
                  {Object.entries(sortOptions).map(([key, option]) => <option key={key} value={key}>{option.label}</option>)}
                </select>
              </label>
            </div>
            {hasFilters && (
              <div className="transaction-filters__summary">
                <span>{listLoading ? 'Filtering...' : listResult?.error ? 'Check the filters' : `${totalCount} matching ${totalCount === 1 ? 'transaction' : 'transactions'}`}</span>
                <button type="button" onClick={() => { setFilters(emptyFilters); setPage(1) }}>Clear filters</button>
              </div>
            )}
            <div className="transaction-list" aria-busy={listLoading}>
              {!listLoading && listResult?.error ? <p className="error" role="alert">{listResult.error}</p>
              : listLoading && !listResult?.data ? <p className="dashboard-empty" role="status">Loading transactions...</p>
              : transactions.length ? transactionsByDay.map(({ day, items: dayTransactions }, index) => {
                const dayTotal = dayTransactions.reduce((total, transaction) => (
                  transaction.type === 'Income' ? total + transaction.amount : total - transaction.amount
                ), 0)
                return (
                  <div className="transaction-day" key={`${day}-${index}`}>
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
              }) : <p className="dashboard-empty">{hasFilters ? 'No transactions match these filters.' : 'No transactions yet.'}</p>}
            </div>
            {totalPages > 1 && (
              <div className="period-picker transaction-pagination">
                <button type="button" onClick={() => setPage(current => current - 1)} aria-label="Previous page" disabled={page <= 1 || listLoading}>‹</button>
                <strong>Page {page} of {totalPages} · {totalCount} transactions</strong>
                <button type="button" onClick={() => setPage(current => current + 1)} aria-label="Next page" disabled={page >= totalPages || listLoading}>›</button>
              </div>
            )}
          </section>
        </>
      )}
      {/* Botao flutuante: fica sempre visivel no fundo do ecra, alinhado com a coluna do Home. */}
      <div className="add-transaction-dock">
        <button
          type="button"
          className="add-transaction-button"
          onClick={openCreateTransaction}
          disabled={busy || !accounts.length || !categories.length}
          aria-label="Add transaction"
          title="Add transaction"
        >
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 5v14M5 12h14" /></svg>
        </button>
      </div>
      {transactionFormOpen && !addingCategory && (
        <div className="account-modal" role="presentation">
          <div className="account-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="transaction-form-title">
            <div className="account-modal__header">
              <div>
                <h3 id="transaction-form-title">{editingTransaction ? 'Edit transaction' : 'New transaction'}</h3>
                <p>{editingTransaction ? 'Update the details of this transaction.' : 'Add the details of your new transaction.'}</p>
              </div>
              <button type="button" className="account-modal__close" aria-label="Close form" onClick={closeTransactionForm} disabled={transactionSaving}>×</button>
            </div>
            <form className="account-form transaction-form" onSubmit={saveTransaction}>
              <div className="account-form__fields">
                <div className="field">
                  <label htmlFor="transaction-type">Type</label>
                  <select id="transaction-type" value={transactionForm.type} onChange={event => {
                    const type = event.target.value as Transaction['type']
                    // Escolhe logo uma categoria do novo tipo (ou deixa "Select a category" se nao houver).
                    setTransactionForm(form => ({ ...form, type, categoryId: categoryFor(type, form.categoryId) }))
                  }}>
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
                    {accountGroups.map(group => (
                      <optgroup key={group.type} label={group.label}>
                        {group.accounts.map(account => <option key={account.id} value={account.id}>{account.name}</option>)}
                      </optgroup>
                    ))}
                  </select>
                </div>
                <div className="field">
                  <div className="category-field-heading">
                    <label htmlFor="transaction-category">Category</label>
                    <button type="button" className="category-add-button" onClick={openAddCategory} disabled={transactionSaving}>+ Add</button>
                  </div>
                  <select id="transaction-category" required value={transactionForm.categoryId} onChange={event => setTransactionForm(form => ({ ...form, categoryId: event.target.value }))}>
                    <option value="" disabled>Select a category</option>
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
                {editingTransaction && (
                  <button
                    type="button"
                    className="transaction-delete-button"
                    onClick={() => setConfirmingDelete(true)}
                    disabled={transactionSaving}
                    aria-label="Delete transaction"
                    title="Delete transaction"
                  >
                    <ActionIcon />
                    <span>Delete transaction</span>
                  </button>
                )}
                <button type="button" className="account-modal__cancel" onClick={closeTransactionForm} disabled={transactionSaving}>Cancel</button>
                <button className="submit account-form__submit" type="submit" disabled={transactionSaving}>
                  {transactionSaving ? 'Saving...' : editingTransaction ? 'Save transaction' : 'Add transaction'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
      {confirmingDelete && editingTransaction && (
        <ConfirmDialog
          title="Delete transaction?"
          message="This transaction will be permanently deleted. This action cannot be undone."
          confirmLabel="Delete transaction"
          busy={transactionSaving}
          onConfirm={() => void deleteTransaction(editingTransaction)}
          onCancel={() => setConfirmingDelete(false)}
        />
      )}
      {transactionFormOpen && addingCategory && (
        <div className="account-modal" role="presentation">
          <div className="account-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="category-form-title">
            <div className="account-modal__header">
              <div>
                <h3 id="category-form-title">Add category</h3>
                <p>Create a personal income or expense category.</p>
              </div>
              <button type="button" className="account-modal__close" aria-label="Close category form" disabled={categorySaving} onClick={() => setAddingCategory(false)}>×</button>
            </div>
            <form className="account-form" onSubmit={saveCategory}>
              <div className="account-form__fields category-form-fields">
                <div className="field">
                  <label htmlFor="category-name">Name</label>
                  <input id="category-name" autoFocus required maxLength={100} placeholder="e.g. Pets" value={categoryName} disabled={categorySaving} onChange={event => setCategoryName(event.target.value)} />
                </div>
                <div className="field">
                  <label htmlFor="category-type">Type</label>
                  <select id="category-type" required value={categoryType} disabled={categorySaving} onChange={event => setCategoryType(event.target.value as Category['type'])}>
                    <option value="Expense">Expense</option>
                    <option value="Income">Income</option>
                  </select>
                </div>
              </div>
              {categoryType !== transactionForm.type && <p className="category-form-note">This category will be available for {categoryType.toLowerCase()} transactions. Your current transaction will remain unchanged.</p>}
              {categoryError && <p className="error" role="alert">{categoryError}</p>}
              <div className="account-modal__footer">
                <button type="button" className="account-modal__cancel" disabled={categorySaving} onClick={() => setAddingCategory(false)}>Cancel</button>
                <button className="submit account-form__submit" type="submit" disabled={categorySaving}>{categorySaving ? 'Saving...' : 'Create category'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

function periodRange({ year, month }: { year: number; month: number }) {
  const prefix = `${year}-${String(month).padStart(2, '0')}`
  return { start: `${prefix}-01`, end: `${prefix}-${new Date(year, month, 0).getDate()}` }
}

function ActionIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M5 7h14M10 4h4l1 3H9l1-3ZM8 10v7m4-7v7m4-7v7M6 7l1 13h10l1-13" />
    </svg>
  )
}

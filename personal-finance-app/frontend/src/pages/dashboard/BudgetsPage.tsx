import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { apiMutation, apiRequest } from '../../api/client'
import { authRequest } from '../auth/auth'
import type { User } from '../auth/auth'
import type { AuthPageMode } from '../auth/AuthPage'
import MainNavigation from './MainNavigation'
import '../../styles/accounts.css'
import '../../styles/budgets.css'
import '../../styles/dashboard.css'

type Budget = {
  id: string
  categoryId: string
  categoryName: string
  month: number
  year: number
  limitAmount: number
}

type Category = {
  id: string
  name: string
  type: 'Income' | 'Expense'
}

type Transaction = {
  categoryId: string
  amount: number
}

const money = new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' })
const monthName = new Intl.DateTimeFormat('pt-PT', { month: 'long', year: 'numeric' })

export default function BudgetsPage({
  user,
  onNavigate,
  onLogout,
}: {
  user: User
  onNavigate: (page: AuthPageMode) => void
  onLogout: () => void
}) {
  const today = new Date()
  const [period, setPeriod] = useState({
    year: today.getFullYear(),
    month: today.getMonth() + 1,
  })
  const [budgets, setBudgets] = useState<Budget[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [spentByCategory, setSpentByCategory] = useState<Record<string, number>>({})
  const [busy, setBusy] = useState(true)
  const [error, setError] = useState('')
  const [reloadVersion, setReloadVersion] = useState(0)
  const [editingBudget, setEditingBudget] = useState<Budget | null>(null)
  const [formCategory, setFormCategory] = useState<Category | null>(null)
  const [formAmount, setFormAmount] = useState('')
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)

  const periodDate = new Date(period.year, period.month - 1, 1)
  const periodStart = `${period.year}-${String(period.month).padStart(2, '0')}-01`
  const periodEnd = `${period.year}-${String(period.month).padStart(2, '0')}-${String(new Date(period.year, period.month, 0).getDate()).padStart(2, '0')}`

  useEffect(() => {
    let active = true
    Promise.all([
      apiRequest<Budget[]>('budgets', period),
      apiRequest<Category[]>('categories'),
      apiRequest<Transaction[]>('transactions', {
        from: periodStart,
        to: periodEnd,
        type: 'Expense',
      }),
    ])
      .then(([loadedBudgets, loadedCategories, transactions]) => {
        if (!active) return
        setBudgets(loadedBudgets)
        setCategories(loadedCategories.filter(category => category.type === 'Expense'))
        setSpentByCategory(transactions.reduce<Record<string, number>>((totals, transaction) => {
          totals[transaction.categoryId] = (totals[transaction.categoryId] ?? 0) + transaction.amount
          return totals
        }, {}))
      })
      .catch(requestError => {
        if (active) {
          setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar os orçamentos.')
        }
      })
      .finally(() => {
        if (active) setBusy(false)
      })
    return () => {
      active = false
    }
  }, [period, periodStart, periodEnd, reloadVersion])

  const budgetByCategory = new Map(budgets.map(budget => [budget.categoryId, budget]))
  const totalBudget = budgets.reduce((total, budget) => total + budget.limitAmount, 0)
  const spentWithinBudgets = budgets.reduce(
    (total, budget) => total + (spentByCategory[budget.categoryId] ?? 0),
    0,
  )

  function changeMonth(direction: -1 | 1) {
    setBusy(true)
    setError('')
    setPeriod(current => {
      const next = new Date(current.year, current.month - 1 + direction, 1)
      return { year: next.getFullYear(), month: next.getMonth() + 1 }
    })
  }

  function openBudgetForm(category: Category) {
    const budget = budgetByCategory.get(category.id) ?? null
    setFormCategory(category)
    setEditingBudget(budget)
    setFormAmount(budget ? String(budget.limitAmount).replace('.', ',') : '')
    setFormError('')
  }

  async function saveBudget(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!formCategory) return

    const normalizedAmount = formAmount.trim().replace(',', '.')
    if (!/^\d+(?:\.\d{1,2})?$/.test(normalizedAmount) || Number(normalizedAmount) <= 0) {
      setFormError('Introduz um valor positivo com, no máximo, duas casas decimais.')
      return
    }

    setSaving(true)
    setFormError('')
    try {
      const request = {
        categoryId: formCategory.id,
        month: period.month,
        year: period.year,
        limitAmount: Number(normalizedAmount),
      }
      await apiMutation<Budget>(
        editingBudget ? `budgets/${editingBudget.id}` : 'budgets',
        editingBudget ? 'PUT' : 'POST',
        request,
      )
      setFormCategory(null)
      setEditingBudget(null)
      setBusy(true)
      setError('')
      setReloadVersion(version => version + 1)
    } catch (requestError) {
      setFormError(requestError instanceof Error ? requestError.message : 'Não foi possível guardar o orçamento.')
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

  return (
    <div className="dashboard budgets-page">
      <header className="dashboard-header">
        <div>
          <span className="eyebrow">PERSONAL FINANCE</span>
          <h2 id="form-title">Orçamentos, {user.name}.</h2>
          <p className="subtitle">Acompanha as tuas despesas face aos limites mensais.</p>
        </div>
        <button className="dashboard-logout" onClick={logout}>Sign out</button>
      </header>
      <MainNavigation page="budgets" onNavigate={onNavigate} />

      <div className="budget-month-picker" aria-label="Navegação entre meses">
        <button type="button" aria-label="Mês anterior" onClick={() => changeMonth(-1)}>‹</button>
        <h3>{monthName.format(periodDate)}</h3>
        <button type="button" aria-label="Mês seguinte" onClick={() => changeMonth(1)}>›</button>
      </div>

      {busy && <p role="status">A carregar os orçamentos...</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {!busy && !error && (
        <>
          <section className="budget-summary" aria-label="Resumo dos orçamentos">
            <article>
              <span>Total orçamentado</span>
              <strong>{money.format(totalBudget)}</strong>
            </article>
            <article>
              <span>Gasto nas categorias orçamentadas</span>
              <strong>{money.format(spentWithinBudgets)}</strong>
            </article>
            <article>
              <span>Disponível</span>
              <strong>{money.format(totalBudget - spentWithinBudgets)}</strong>
            </article>
          </section>

          <section className="dashboard-section">
            <div className="dashboard-section-title">
              <h3>Categorias de despesa <span className="section-count">{categories.length}</span></h3>
            </div>
            {categories.length > 0 ? (
              <div className="budget-list">
                {categories.map(category => {
                  const budget = budgetByCategory.get(category.id)
                  const spent = spentByCategory[category.id] ?? 0
                  const percentage = budget ? spent / budget.limitAmount * 100 : 0
                  const status = percentage >= 100 ? 'exceeded' : percentage >= 80 ? 'near' : 'normal'
                  return (
                    <article className="budget-row" key={category.id}>
                      <div className="budget-row__header">
                        <h4>{category.name}</h4>
                        <button type="button" onClick={() => openBudgetForm(category)}>
                          {budget ? 'Editar orçamento' : 'Definir orçamento'}
                        </button>
                      </div>
                      {budget ? (
                        <>
                          <div
                            className={`budget-progress budget-progress--${status}`}
                            role="progressbar"
                            aria-label={`Orçamento de ${category.name}`}
                            aria-valuemin={0}
                            aria-valuemax={100}
                            aria-valuenow={Math.min(percentage, 100)}
                            aria-valuetext={`${money.format(spent)} de ${money.format(budget.limitAmount)}`}
                          >
                            <span style={{ width: `${Math.min(percentage, 100)}%` }} />
                          </div>
                          <div className="budget-row__details">
                            <span>{money.format(spent)} de {money.format(budget.limitAmount)}</span>
                            <span className={`budget-row__status budget-row__status--${status}`}>
                              {status === 'exceeded'
                                ? 'Orçamento ultrapassado'
                                : status === 'near'
                                  ? 'A aproximar-se do limite'
                                  : 'Dentro do limite'}
                              {' · '}{Math.round(percentage)}%
                            </span>
                          </div>
                        </>
                      ) : (
                        <p className="budget-row__no-budget">
                          {money.format(spent)} gastos · Sem orçamento definido
                        </p>
                      )}
                    </article>
                  )
                })}
              </div>
            ) : (
              <p className="dashboard-empty">Ainda não existem categorias de despesa.</p>
            )}
          </section>
        </>
      )}

      {formCategory && (
        <div
          className="account-modal"
          role="presentation"
          onMouseDown={event => {
            if (event.target === event.currentTarget && !saving) {
              setFormCategory(null)
              setEditingBudget(null)
            }
          }}
        >
          <div className="account-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="budget-form-title">
            <div className="account-modal__header">
              <div>
                <h3 id="budget-form-title">{editingBudget ? 'Editar orçamento' : 'Definir orçamento'}</h3>
                <p>{formCategory.name} · {monthName.format(periodDate)}</p>
              </div>
              <button
                type="button"
                className="account-modal__close"
                aria-label="Fechar formulário"
                onClick={() => {
                  setFormCategory(null)
                  setEditingBudget(null)
                }}
                disabled={saving}
              >
                ×
              </button>
            </div>
            <form className="account-form budget-form" onSubmit={event => void saveBudget(event)}>
              <div className="field">
                <label htmlFor="budget-limit">Limite mensal (€)</label>
                <input
                  id="budget-limit"
                  inputMode="decimal"
                  value={formAmount}
                  onChange={event => setFormAmount(event.target.value)}
                  placeholder="Ex.: 200,00"
                  required
                />
              </div>
              {formError && <p className="error" role="alert">{formError}</p>}
              <div className="account-modal__footer">
                <button
                  type="button"
                  className="account-modal__cancel"
                  onClick={() => {
                    setFormCategory(null)
                    setEditingBudget(null)
                  }}
                  disabled={saving}
                >
                  Cancelar
                </button>
                <button className="submit account-form__submit" type="submit" disabled={saving}>
                  {saving ? 'A guardar...' : 'Guardar orçamento'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

// Lista unica dos tipos de conta (igual ao enum AccountType do backend).
// Usada na pagina Accounts (criar/editar conta) e no Home (filtro e edicao de transacoes).
export type AccountType = 'Checking' | 'Savings' | 'Cash' | 'CreditCard' | 'Investment' | 'Loan' | 'Other'

export const accountTypeLabels: Record<AccountType, string> = {
  Checking: 'Checking',
  Savings: 'Savings',
  Cash: 'Cash',
  CreditCard: 'Credit card',
  Investment: 'Investment',
  Loan: 'Loan',
  Other: 'Other',
}

export const accountTypes = Object.keys(accountTypeLabels) as AccountType[]

// Agrupa contas pelo tipo, pela ordem da lista acima (tipos sem contas ficam de fora).
export function groupAccountsByType<T extends { type: AccountType; name: string }>(accounts: T[]) {
  return accountTypes
    .map(type => ({
      type,
      label: accountTypeLabels[type],
      accounts: accounts
        .filter(account => account.type === type)
        .sort((a, b) => a.name.localeCompare(b.name)),
    }))
    .filter(group => group.accounts.length > 0)
}

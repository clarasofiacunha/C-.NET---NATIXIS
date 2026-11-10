using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Summary;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Services;

public sealed class MonthlySummaryService(AppDbContext db) : IMonthlySummaryService
{
    public async Task<MonthlySummaryResponse> GetAsync(
        Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        // O saldo total representa o saldo contabilístico atual de todas as contas:
        // saldos iniciais + todas as receitas registadas - todas as despesas registadas.
        // (não depende do mês escolhido para as outras métricas)
        var initialBalances = await db.Accounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .SumAsync(a => (decimal?)a.InitialBalance, cancellationToken) ?? 0m;

        var ownTransactions = db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId);

        var totalIncome = await ownTransactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
        var totalExpenses = await ownTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        // intervalo UTC semiaberto evita excluir o último dia do mês
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        // 9999-12 é válido: não existe um primeiro dia do mês seguinte.
        DateTime? endExclusive = year == 9999 && month == 12
            ? null
            : start.AddMonths(1);

        var monthlyExpensesQuery = ownTransactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= start);
        if (endExclusive.HasValue)
            monthlyExpensesQuery = monthlyExpensesQuery.Where(t => t.Date < endExclusive.Value);

        var monthlyExpenses = await monthlyExpensesQuery
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var monthlyBudgets = await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Select(b => new { b.CategoryId, b.LimitAmount })
            .ToListAsync(cancellationToken);
        var totalBudget = monthlyBudgets.Sum(b => b.LimitAmount);

        // Só as despesas de categorias com orçamento neste mês entram na percentagem.
        // Despesas sem orçamento continuam a entrar em MonthlyExpenses.
        var budgetedCategoryIds = monthlyBudgets.Select(b => b.CategoryId).ToArray();
        var budgetedExpenses = budgetedCategoryIds.Length == 0
            ? 0m
            : await monthlyExpensesQuery
                .Where(t => budgetedCategoryIds.Contains(t.CategoryId))
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var usagePercentage = totalBudget == 0m
            ? 0m
            : decimal.Round(budgetedExpenses / totalBudget * 100m, 2,
                MidpointRounding.AwayFromZero);

        return new MonthlySummaryResponse(
            year,
            month,
            initialBalances + totalIncome - totalExpenses,
            monthlyExpenses,
            totalBudget,
            budgetedExpenses,
            usagePercentage);
    }
}

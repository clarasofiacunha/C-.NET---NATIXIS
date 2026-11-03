namespace PersonalFinance.Api.DTOs.Summary;

/// <summary>Indicadores financeiros do utilizador autenticado para um mês.</summary>
public sealed record MonthlySummaryResponse(
    int Year,
    int Month,
    decimal TotalBalance,
    decimal MonthlyExpenses,
    decimal TotalBudget,
    decimal BudgetedExpenses,
    decimal BudgetUsagePercentage);

namespace PersonalFinance.Api.DTOs.Budgets;

/// <summary>Monthly spending limit for one expense category.</summary>
public sealed record BudgetResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    int Month,
    int Year,
    decimal LimitAmount);

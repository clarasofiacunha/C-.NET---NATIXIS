using PersonalFinance.Api.DTOs.Budgets;

namespace PersonalFinance.Api.Services;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetResponse>> GetAllAsync(
        Guid userId, int? year, int? month, CancellationToken cancellationToken);

    Task<BudgetResponse?> GetByIdAsync(
        Guid userId, Guid id, CancellationToken cancellationToken);

    Task<BudgetOperationResult> CreateAsync(
        Guid userId, CreateBudgetRequest request, CancellationToken cancellationToken);

    Task<BudgetOperationResult> UpdateAsync(
        Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}

public enum BudgetOperationStatus
{
    Success,
    NotFound,
    Duplicate
}

public sealed record BudgetOperationResult(
    BudgetOperationStatus Status,
    BudgetResponse? Budget = null);

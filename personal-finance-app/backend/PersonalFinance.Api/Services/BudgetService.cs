using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Budgets;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Services;

public sealed class BudgetService(AppDbContext db) : IBudgetService
{
    private const string BudgetUniqueIndex = "IX_Budgets_UserId_CategoryId_Year_Month";

    public async Task<IReadOnlyList<BudgetResponse>> GetAllAsync(
        Guid userId, int? year, int? month, CancellationToken cancellationToken)
    {
        return await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId)
            .Where(b => year == null || b.Year == year)
            .Where(b => month == null || b.Month == month)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .Select(b => new BudgetResponse(
                b.Id, b.CategoryId, b.Category.Name, b.Month, b.Year, b.LimitAmount))
            .ToListAsync(cancellationToken);
    }

    public Task<BudgetResponse?> GetByIdAsync(
        Guid userId, Guid id, CancellationToken cancellationToken)
    {
        return db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Id == id)
            .Select(b => new BudgetResponse(
                b.Id, b.CategoryId, b.Category.Name, b.Month, b.Year, b.LimitAmount))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BudgetAlertResponse>> GetAlertsAsync(
        Guid userId, int? year, int? month, CancellationToken cancellationToken)
    {
        var budgets = await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId)
            .Where(b => year == null || b.Year == year)
            .Where(b => month == null || b.Month == month)
            .Select(b => new
            {
                b.Id,
                b.CategoryId,
                CategoryName = b.Category.Name,
                b.Month,
                b.Year,
                b.LimitAmount,
                SpentAmount = db.Transactions.AsNoTracking()
                    .Where(t => t.UserId == userId
                        && t.CategoryId == b.CategoryId
                        && t.Type == TransactionType.Expense
                        && t.Date.Year == b.Year
                        && t.Date.Month == b.Month)
                    .Sum(t => (decimal?)t.Amount) ?? 0m
            })
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.CategoryName)
            .ToListAsync(cancellationToken);

        return budgets
            .Select(b => (Budget: b, Status: BudgetAlertClassifier.Classify(b.LimitAmount, b.SpentAmount)))
            .Where(x => x.Status is not null)
            .Select(x => new BudgetAlertResponse(
                x.Budget.Id,
                x.Budget.CategoryId,
                x.Budget.CategoryName,
                x.Budget.Month,
                x.Budget.Year,
                x.Budget.LimitAmount,
                x.Budget.SpentAmount,
                x.Budget.LimitAmount - x.Budget.SpentAmount,
                decimal.Round(x.Budget.SpentAmount / x.Budget.LimitAmount * 100m, 2),
                x.Status!.Value))
            .ToList();
    }

    public async Task<BudgetOperationResult> CreateAsync(
        Guid userId, CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var category = await GetExpenseCategoryAsync(userId, request.CategoryId, cancellationToken);
        if (category is null)
            return new BudgetOperationResult(BudgetOperationStatus.NotFound);

        var exists = await db.Budgets.AnyAsync(b =>
            b.UserId == userId
            && b.CategoryId == request.CategoryId
            && b.Year == request.Year
            && b.Month == request.Month, cancellationToken);
        if (exists)
            return new BudgetOperationResult(BudgetOperationStatus.Duplicate);

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = category.Id,
            Month = request.Month,
            Year = request.Year,
            LimitAmount = request.LimitAmount
        };
        db.Budgets.Add(budget);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsBudgetUniqueViolation(exception))
        {
            return new BudgetOperationResult(BudgetOperationStatus.Duplicate);
        }

        return new BudgetOperationResult(
            BudgetOperationStatus.Success,
            new BudgetResponse(
                budget.Id, category.Id, category.Name, budget.Month, budget.Year, budget.LimitAmount));
    }

    public async Task<BudgetOperationResult> UpdateAsync(
        Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await db.Budgets
            .SingleOrDefaultAsync(b => b.UserId == userId && b.Id == id, cancellationToken);
        if (budget is null)
            return new BudgetOperationResult(BudgetOperationStatus.NotFound);

        var category = await GetExpenseCategoryAsync(userId, request.CategoryId, cancellationToken);
        if (category is null)
            return new BudgetOperationResult(BudgetOperationStatus.NotFound);

        var exists = await db.Budgets.AnyAsync(b =>
            b.UserId == userId
            && b.Id != id
            && b.CategoryId == request.CategoryId
            && b.Year == request.Year
            && b.Month == request.Month, cancellationToken);
        if (exists)
            return new BudgetOperationResult(BudgetOperationStatus.Duplicate);

        budget.CategoryId = category.Id;
        budget.Month = request.Month;
        budget.Year = request.Year;
        budget.LimitAmount = request.LimitAmount;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsBudgetUniqueViolation(exception))
        {
            return new BudgetOperationResult(BudgetOperationStatus.Duplicate);
        }

        return new BudgetOperationResult(
            BudgetOperationStatus.Success,
            new BudgetResponse(
                budget.Id, category.Id, category.Name, budget.Month, budget.Year, budget.LimitAmount));
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var budget = await db.Budgets
            .SingleOrDefaultAsync(b => b.UserId == userId && b.Id == id, cancellationToken);
        if (budget is null)
            return false;

        db.Budgets.Remove(budget);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<Category?> GetExpenseCategoryAsync(
        Guid userId, Guid categoryId, CancellationToken cancellationToken)
    {
        return db.Categories.SingleOrDefaultAsync(
            c => c.UserId == userId
                && c.Id == categoryId
                && c.Type == TransactionType.Expense,
            cancellationToken);
    }

    private static bool IsBudgetUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: BudgetUniqueIndex
        };
    }
}

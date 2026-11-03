using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Categories;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Services;

public sealed class CategoryService(AppDbContext db) : ICategoryService
{
    private const string CategoryUniqueIndex = "IX_Categories_UserId_Name_Type";

    public async Task<IReadOnlyList<CategoryResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        return await db.Categories.AsNoTracking()
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponseDto(
                category.Id, category.Name, category.Type))
            .ToListAsync(cancellationToken);
    }

    public Task<CategoryResponseDto?> GetByIdAsync(
        Guid userId, Guid id, CancellationToken cancellationToken)
    {
        return db.Categories.AsNoTracking()
            .Where(category => category.UserId == userId && category.Id == id)
            .Select(category => new CategoryResponseDto(
                category.Id, category.Name, category.Type))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CategoryOperationResult> CreateAsync(
        Guid userId, CategoryRequestDto request, CancellationToken cancellationToken)
    {
        var name = request.Name!.Trim();
        if (await CategoryExistsAsync(userId, name, request.Type!.Value, null, cancellationToken))
            return new CategoryOperationResult(CategoryOperationStatus.Duplicate);

        var category = new Category
        {
            UserId = userId,
            Name = name,
            Type = request.Type.Value
        };
        db.Categories.Add(category);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsCategoryUniqueViolation(exception))
        {
            return new CategoryOperationResult(CategoryOperationStatus.Duplicate);
        }

        return new CategoryOperationResult(
            CategoryOperationStatus.Success, ToResponse(category));
    }

    public async Task<CategoryOperationResult> UpdateAsync(
        Guid userId, Guid id, CategoryRequestDto request, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.Id == id,
                cancellationToken);
        if (category is null)
            return new CategoryOperationResult(CategoryOperationStatus.NotFound);

        if (category.Type != request.Type!.Value
            && await HasReferencesAsync(userId, id, cancellationToken))
        {
            return new CategoryOperationResult(CategoryOperationStatus.InUse);
        }

        var name = request.Name!.Trim();
        if (await CategoryExistsAsync(userId, name, request.Type.Value, id, cancellationToken))
            return new CategoryOperationResult(CategoryOperationStatus.Duplicate);

        category.Name = name;
        category.Type = request.Type.Value;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsCategoryUniqueViolation(exception))
        {
            return new CategoryOperationResult(CategoryOperationStatus.Duplicate);
        }

        return new CategoryOperationResult(
            CategoryOperationStatus.Success, ToResponse(category));
    }

    public async Task<CategoryDeleteStatus> DeleteAsync(
        Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.Id == id,
                cancellationToken);
        if (category is null)
            return CategoryDeleteStatus.NotFound;

        if (await HasReferencesAsync(userId, id, cancellationToken))
            return CategoryDeleteStatus.InUse;

        db.Categories.Remove(category);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsCategoryForeignKeyViolation(exception))
        {
            return CategoryDeleteStatus.InUse;
        }

        return CategoryDeleteStatus.Success;
    }

    private Task<bool> CategoryExistsAsync(
        Guid userId,
        string name,
        TransactionType type,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        return db.Categories.AnyAsync(category =>
            category.UserId == userId
            && category.Name == name
            && category.Type == type
            && (excludedId == null || category.Id != excludedId), cancellationToken);
    }

    private async Task<bool> HasReferencesAsync(
        Guid userId, Guid categoryId, CancellationToken cancellationToken)
    {
        return await db.Transactions.AnyAsync(
                transaction => transaction.UserId == userId
                    && transaction.CategoryId == categoryId,
                cancellationToken)
            || await db.Budgets.AnyAsync(
                budget => budget.UserId == userId
                    && budget.CategoryId == categoryId,
                cancellationToken);
    }

    private static CategoryResponseDto ToResponse(Category category) =>
        new(category.Id, category.Name, category.Type);

    private static bool IsCategoryUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CategoryUniqueIndex
        };

    private static bool IsCategoryForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "FK_Budgets_Categories_UserId_CategoryId"
                or "FK_Transactions_Categories_UserId_CategoryId"
        };
}

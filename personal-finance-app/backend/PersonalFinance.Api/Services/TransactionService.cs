using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs;
using PersonalFinance.Api.DTOs.Transactions;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Services;

public sealed class TransactionService(AppDbContext db) : ITransactionService
{
    public Task<PagedResponse<TransactionResponseDto>> GetAllAsync(
        Guid userId, TransactionQueryParameters query, CancellationToken cancellationToken) =>
        db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .ApplyFilters(query)
            .ToPagedResponseAsync(query, cancellationToken);

    public Task<TransactionResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        db.Transactions.AsNoTracking()
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => ToResponse(t, t.Account.Name, t.Category.Name))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<TransactionOperationResult> CreateAsync(
        Guid userId, TransactionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request, out var error)) return new(null, error, true);
        var references = await GetReferencesAsync(userId, request, cancellationToken);
        if (references is null) return new(null, "The account or category was not found.");
        // if (references.Value.CategoryType != request.Type!.Value) return new(null, "The transaction type must match the category type.", true);

        var transaction = new FinanceTransaction
        {
            UserId = userId, AccountId = request.AccountId!.Value, CategoryId = request.CategoryId!.Value,
            Amount = request.Amount!.Value, Type = request.Type!.Value,
            Date = request.Date!.Value.UtcDateTime, Description = NormalizeDescription(request.Description)
        };
        db.Transactions.Add(transaction);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        { return new(null, "The account or category no longer exists."); }
        return new(ToResponse(transaction, references.Value.AccountName, references.Value.CategoryName), null);
    }

    public async Task<TransactionOperationResult> UpdateAsync(
        Guid userId, Guid id, TransactionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request, out var error)) return new(null, error, true);
        var transaction = await db.Transactions.FirstOrDefaultAsync(
            t => t.Id == id && t.UserId == userId, cancellationToken);
        if (transaction is null) return new(null, null);
        var references = await GetReferencesAsync(userId, request, cancellationToken);
        if (references is null) return new(null, "The account or category was not found.");
        // if (references.Value.CategoryType != request.Type!.Value) return new(null, "The transaction type must match the category type.", true);

        transaction.AccountId = request.AccountId!.Value; transaction.CategoryId = request.CategoryId!.Value;
        transaction.Amount = request.Amount!.Value; transaction.Type = request.Type!.Value;
        transaction.Date = request.Date!.Value.UtcDateTime;
        transaction.Description = NormalizeDescription(request.Description);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return new(null, null); }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        { return new(null, "The account or category no longer exists."); }
        return new(ToResponse(transaction, references.Value.AccountName, references.Value.CategoryName), null);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(
            t => t.Id == id && t.UserId == userId, cancellationToken);
        if (transaction is null) return false;
        db.Transactions.Remove(transaction);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return false; }
        return true;
    }

    private async Task<(string AccountName, string CategoryName, TransactionType CategoryType)?> GetReferencesAsync(
        Guid userId, TransactionRequestDto request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AsNoTracking().Where(a =>
            a.Id == request.AccountId!.Value && a.UserId == userId).Select(a => a.Name).FirstOrDefaultAsync(cancellationToken);
        var category = await db.Categories.AsNoTracking().Where(c =>
            c.Id == request.CategoryId!.Value && c.UserId == userId).Select(c => new { c.Name, c.Type }).FirstOrDefaultAsync(cancellationToken);
        return account is null || category is null ? null : (account, category.Name, category.Type);
    }

    private static TransactionResponseDto ToResponse(FinanceTransaction t, string accountName, string categoryName) =>
        new(t.Id, t.AccountId, accountName, t.CategoryId, categoryName, t.Amount, t.Type, t.Date, t.Description);

    private static string? NormalizeDescription(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsForeignKeyViolation(DbUpdateException ex) => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };

    private static bool TryValidateRequest(TransactionRequestDto request, out string? error)
    {
        if (request.AccountId is null || request.AccountId == Guid.Empty || request.CategoryId is null || request.CategoryId == Guid.Empty)
            { error = "The account and category must have valid identifiers."; return false; }
        if (request.Type is null || !Enum.IsDefined(request.Type.Value))
            { error = "The type must be Income or Expense."; return false; }
        if (request.Amount is null || request.Amount <= 0m || request.Amount > 9999999999999999.99m || decimal.Round(request.Amount.Value, 2) != request.Amount.Value)
            { error = "The amount must be positive and have at most two decimal places."; return false; }
        if (request.Date is null) { error = "The date is required."; return false; }
        if (request.Description?.Length > 500) { error = "The description cannot exceed 500 characters."; return false; }
        error = null; return true;
    }
}
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Transactions;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Services;

public sealed class TransactionService(AppDbContext db) : ITransactionService
{
    public async Task<List<TransactionResponseDto>> GetAllAsync(
        Guid userId, DateOnly? from, DateOnly? to, TransactionType? type,
        Guid? accountId, Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = db.Transactions.AsNoTracking().Where(t => t.UserId == userId);
        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (to.HasValue && to.Value < DateOnly.MaxValue)
            query = query.Where(t => t.Date < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);

        return await query.OrderByDescending(t => t.Date).ThenBy(t => t.Id)
            .Select(t => ToResponse(t, t.Account.Name, t.Category.Name))
            .ToListAsync(cancellationToken);
    }

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
        if (references is null) return new(null, "A conta ou a categoria nao foi encontrada.");
        // if (references.Value.CategoryType != request.Type!.Value) return new(null, "O tipo da transacao deve coincidir com o tipo da categoria.", true);

        var transaction = new FinanceTransaction
        {
            UserId = userId, AccountId = request.AccountId!.Value, CategoryId = request.CategoryId!.Value,
            Amount = request.Amount!.Value, Type = request.Type.Value,
            Date = request.Date!.Value.UtcDateTime, Description = NormalizeDescription(request.Description)
        };
        db.Transactions.Add(transaction);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        { return new(null, "A conta ou a categoria ja nao existe."); }
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
        if (references is null) return new(null, "A conta ou a categoria nao foi encontrada.");
        // if (references.Value.CategoryType != request.Type!.Value) return new(null, "O tipo da transacao deve coincidir com o tipo da categoria.", true);

        transaction.AccountId = request.AccountId!.Value; transaction.CategoryId = request.CategoryId!.Value;
        transaction.Amount = request.Amount!.Value; transaction.Type = request.Type.Value;
        transaction.Date = request.Date!.Value.UtcDateTime;
        transaction.Description = NormalizeDescription(request.Description);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return new(null, null); }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        { return new(null, "A conta ou a categoria ja nao existe."); }
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
            { error = "A conta e a categoria devem ter identificadores validos."; return false; }
        if (request.Type is null || !Enum.IsDefined(request.Type.Value))
            { error = "O tipo deve ser Income ou Expense."; return false; }
        if (request.Amount is null || request.Amount <= 0m || request.Amount > 9999999999999999.99m || decimal.Round(request.Amount.Value, 2) != request.Amount.Value)
            { error = "O montante deve ser positivo e ter, no maximo, duas casas decimais."; return false; }
        if (request.Date is null) { error = "A data e obrigatoria."; return false; }
        if (request.Description?.Length > 500) { error = "A descricao nao pode exceder 500 caracteres."; return false; }
        error = null; return true;
    }
}
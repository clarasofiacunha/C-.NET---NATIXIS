using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Accounts;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Services;

public sealed class AccountService(AppDbContext db) : IAccountService
{
    public Task<List<AccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(a => new AccountResponseDto(a.Id, a.Name, a.Type, a.InitialBalance, a.Balance, a.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<AccountResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking()
            .Where(a => a.Id == id && a.UserId == userId)
            .Select(a => new AccountResponseDto(a.Id, a.Name, a.Type, a.InitialBalance, a.Balance, a.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AccountOperationResult> CreateAsync(
        Guid userId, AccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request, out var error))
            return new(null, error, true);

        var account = new Account
        {
            UserId = userId,
            Name = request.Name!.Trim(),
            Type = request.Type!.Value,
            InitialBalance = request.InitialBalance!.Value
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        return new(AccountResponseDto.FromModel(account), null);
    }

    public async Task<AccountOperationResult> UpdateAsync(
        Guid userId, Guid id, AccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request, out var error))
            return new(null, error, true);

        var account = await db.Accounts.FirstOrDefaultAsync(
            a => a.Id == id && a.UserId == userId, cancellationToken);
        if (account is null) return new(null, null);

        account.Name = request.Name!.Trim();
        account.Type = request.Type!.Value;
        account.InitialBalance = request.InitialBalance!.Value;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(null, null);
        }
        return new(AccountResponseDto.FromModel(account), null);
    }

    public async Task<AccountDeleteResult> DeleteAsync(
        Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(
            a => a.Id == id && a.UserId == userId, cancellationToken);
        if (account is null) return AccountDeleteResult.NotFound;
        if (await db.Transactions.AnyAsync(
            t => t.UserId == userId && t.AccountId == id, cancellationToken))
            return AccountDeleteResult.HasTransactions;

        db.Accounts.Remove(account);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AccountDeleteResult.NotFound;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return AccountDeleteResult.HasTransactions;
        }
        return AccountDeleteResult.Deleted;
    }

    private static bool TryValidateRequest(AccountRequestDto request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "O nome da conta nao pode estar vazio.";
            return false;
        }
        if (request.Type is null || !Enum.IsDefined(request.Type.Value))
        {
            error = "O tipo deve ser Checking, Savings ou Cash.";
            return false;
        }
        if (request.InitialBalance is null ||
            decimal.Round(request.InitialBalance.Value, 2) != request.InitialBalance.Value)
        {
            error = "O saldo inicial deve ter, no maximo, duas casas decimais.";
            return false;
        }
        error = null;
        return true;
    }
}
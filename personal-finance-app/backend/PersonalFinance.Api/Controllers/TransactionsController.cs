using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs;
using PersonalFinance.Api.DTOs.Transactions;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(AppDbContext db) : ControllerBase
{
    // O utilizador autenticado e a unica fonte fiavel do UserId.
    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // Lista paginada, com filtros e ordenacao. Ver TransactionQueryParameters.
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TransactionResponseDto>>> GetAll(
        [FromQuery] TransactionQueryParameters query,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (query.Validate() is { } error) return BadRequest(new { error });

        var page = await db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .ApplyFilters(query)
            .ToPagedResponseAsync(query, cancellationToken);

        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponseDto>> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var transaction = await db.Transactions.AsNoTracking()
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => new TransactionResponseDto(
                t.Id, t.AccountId, t.Account.Name, t.CategoryId, t.Category.Name,
                t.Amount, t.Type, t.Date, t.Description))
            .FirstOrDefaultAsync(cancellationToken);

        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> Create(
        TransactionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryValidateRequest(request, out var values, out var error))
            return BadRequest(new { error });

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == values.AccountId && a.UserId == userId,
                cancellationToken);
        if (account is null) return NotFound(new { error = "Account not found." });

        var category = await db.Categories.AsNoTracking()
            .Where(c => c.Id == values.CategoryId && c.UserId == userId)
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (category is null) return NotFound(new { error = "Category not found." });

        var transaction = new FinanceTransaction
        {
            UserId = userId,
            AccountId = values.AccountId,
            CategoryId = values.CategoryId,
            Amount = values.Amount,
            Type = values.Type,
            Date = values.Date.UtcDateTime,
            Description = NormalizeDescription(request.Description)
        };

        account.Balance += BalanceChange(values.Type, values.Amount);
        account.Version++;
        db.Transactions.Add(transaction);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "The account was changed at the same time. Please try again." });
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return NotFound(new { error = "The account or category no longer exists." });
        }

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id },
            ToResponse(transaction, account.Name, category.Name));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionResponseDto>> Update(
        Guid id, TransactionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryValidateRequest(request, out var values, out var error))
            return BadRequest(new { error });

        var transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (transaction is null) return NotFound();

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == values.AccountId && a.UserId == userId,
                cancellationToken);
        if (account is null) return NotFound(new { error = "Account not found." });

        var category = await db.Categories.AsNoTracking()
            .Where(c => c.Id == values.CategoryId && c.UserId == userId)
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (category is null) return NotFound(new { error = "Category not found." });

        var previousAccount = transaction.AccountId == account.Id
            ? account
            : await db.Accounts.FirstOrDefaultAsync(
                a => a.Id == transaction.AccountId && a.UserId == userId, cancellationToken);
        if (previousAccount is null)
            return NotFound(new { error = "The account linked to this transaction was not found." });

        if (previousAccount.Id == account.Id)
        {
            account.Balance += BalanceChange(values.Type, values.Amount)
                - BalanceChange(transaction.Type, transaction.Amount);
            account.Version++;
        }
        else
        {
            previousAccount.Balance -= BalanceChange(transaction.Type, transaction.Amount);
            previousAccount.Version++;
            account.Balance += BalanceChange(values.Type, values.Amount);
            account.Version++;
        }

        transaction.AccountId = values.AccountId;
        transaction.CategoryId = values.CategoryId;
        transaction.Amount = values.Amount;
        transaction.Type = values.Type;
        transaction.Date = values.Date.UtcDateTime;
        transaction.Description = NormalizeDescription(request.Description);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "The account or transaction was changed at the same time. Please try again." });
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return NotFound(new { error = "The account or category no longer exists." });
        }

        return Ok(ToResponse(transaction, account.Name, category.Name));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (transaction is null) return NotFound();

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == transaction.AccountId && a.UserId == userId,
                cancellationToken);
        if (account is null)
            return NotFound(new { error = "The account linked to this transaction was not found." });

        account.Balance -= BalanceChange(transaction.Type, transaction.Amount);
        account.Version++;
        db.Transactions.Remove(transaction);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "The account or transaction was changed at the same time. Please try again." });
        }

        return NoContent();
    }

    private static TransactionResponseDto ToResponse(
        FinanceTransaction t, string accountName, string categoryName) =>
        new(t.Id, t.AccountId, accountName, t.CategoryId, categoryName,
            t.Amount, t.Type, t.Date, t.Description);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static decimal BalanceChange(TransactionType type, decimal amount) =>
        type == TransactionType.Income ? amount : -amount;

    private static bool IsForeignKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.ForeignKeyViolation };

    private static bool TryValidateRequest(
        TransactionRequestDto request,
        out ValidatedTransactionRequest values,
        out string? error)
    {
        if (request.AccountId is not Guid accountId || accountId == Guid.Empty ||
            request.CategoryId is not Guid categoryId || categoryId == Guid.Empty)
        {
            values = default;
            error = "The account and category must have valid identifiers.";
            return false;
        }
        if (request.Type is not TransactionType type || !Enum.IsDefined(type))
        {
            values = default;
            error = "The type must be Income or Expense.";
            return false;
        }
        if (request.Amount is not decimal amount || amount <= 0m ||
            amount > 9999999999999999.99m ||
            decimal.Round(amount, 2) != amount)
        {
            values = default;
            error = "The amount must be positive and have at most two decimal places.";
            return false;
        }
        if (request.Date is not DateTimeOffset date)
        {
            values = default;
            error = "The date is required.";
            return false;
        }
        if (request.Description?.Length > 500)
        {
            values = default;
            error = "The description cannot exceed 500 characters.";
            return false;
        }

        values = new ValidatedTransactionRequest(accountId, categoryId, amount, type, date);
        error = null;
        return true;
    }

    private readonly record struct ValidatedTransactionRequest(
        Guid AccountId,
        Guid CategoryId,
        decimal Amount,
        TransactionType Type,
        DateTimeOffset Date);
}

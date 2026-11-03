using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
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

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetAll(
        [FromQuery(Name = "from")] DateOnly? from,
        [FromQuery(Name = "to")] DateOnly? to,
        [FromQuery] TransactionType? type,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (from > to) return BadRequest(new { error = "A data inicial deve ser anterior ou igual a data final." });
        if (type.HasValue && !Enum.IsDefined(type.Value))
            return BadRequest(new { error = "O tipo deve ser Income ou Expense." });

        var query = db.Transactions.AsNoTracking().Where(t => t.UserId == userId);

        // Intervalo [00:00 UTC do inicio, 00:00 UTC do dia seguinte ao fim).
        // Isto inclui todas as horas do ultimo dia e permite usar o indice por data.
        if (from.HasValue)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.Date >= start);
        }
        if (to.HasValue && to.Value < DateOnly.MaxValue)
        {
            var endExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.Date < endExclusive);
        }
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Id)
            .Select(t => new TransactionResponseDto(
                t.Id, t.AccountId, t.Account.Name, t.CategoryId, t.Category.Name,
                t.Amount, t.Type, t.Date, t.Description))
            .ToListAsync(cancellationToken);

        return Ok(transactions);
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

        var account = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == values.AccountId && a.UserId == userId)
            .Select(a => new { a.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (account is null) return NotFound(new { error = "Conta nao encontrada." });

        var category = await db.Categories.AsNoTracking()
            .Where(c => c.Id == values.CategoryId && c.UserId == userId)
            .Select(c => new { c.Name, c.Type })
            .FirstOrDefaultAsync(cancellationToken);
        if (category is null) return NotFound(new { error = "Categoria nao encontrada." });
        if (category.Type != values.Type)
            return BadRequest(new { error = "O tipo da transacao deve coincidir com o tipo da categoria." });

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

        db.Transactions.Add(transaction);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return NotFound(new { error = "A conta ou a categoria ja nao existe." });
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

        var account = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == values.AccountId && a.UserId == userId)
            .Select(a => new { a.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (account is null) return NotFound(new { error = "Conta nao encontrada." });

        var category = await db.Categories.AsNoTracking()
            .Where(c => c.Id == values.CategoryId && c.UserId == userId)
            .Select(c => new { c.Name, c.Type })
            .FirstOrDefaultAsync(cancellationToken);
        if (category is null) return NotFound(new { error = "Categoria nao encontrada." });
        if (category.Type != values.Type)
            return BadRequest(new { error = "O tipo da transacao deve coincidir com o tipo da categoria." });

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
            return NotFound();
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return NotFound(new { error = "A conta ou a categoria ja nao existe." });
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

        db.Transactions.Remove(transaction);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static TransactionResponseDto ToResponse(
        FinanceTransaction t, string accountName, string categoryName) =>
        new(t.Id, t.AccountId, accountName, t.CategoryId, categoryName,
            t.Amount, t.Type, t.Date, t.Description);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

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
            error = "A conta e a categoria devem ter identificadores validos.";
            return false;
        }
        if (request.Type is not TransactionType type || !Enum.IsDefined(type))
        {
            values = default;
            error = "O tipo deve ser Income ou Expense.";
            return false;
        }
        if (request.Amount is not decimal amount || amount <= 0m ||
            amount > 9999999999999999.99m ||
            decimal.Round(amount, 2) != amount)
        {
            values = default;
            error = "O montante deve ser positivo e ter, no maximo, duas casas decimais.";
            return false;
        }
        if (request.Date is not DateTimeOffset date)
        {
            values = default;
            error = "A data e obrigatoria.";
            return false;
        }
        if (request.Description?.Length > 500)
        {
            values = default;
            error = "A descricao nao pode exceder 500 caracteres.";
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

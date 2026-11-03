using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Accounts;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Authorize] //[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
[Route("api/accounts")]
public sealed class AccountsController(AppDbContext db) : ControllerBase
{
    // O UserId vem da sessao autenticada, nunca do corpo ou URL do pedido.
    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(a => new AccountResponseDto(
                a.Id, a.Name, a.Type, a.InitialBalance, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponseDto>> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var account = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == id && a.UserId == userId)
            .Select(a => new AccountResponseDto(
                a.Id, a.Name, a.Type, a.InitialBalance, a.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null) return NotFound();
        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create(
        AccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryValidateRequest(request, out var error)) return BadRequest(new { error });

        var account = new Account
        {
            UserId = userId,
            Name = request.Name!.Trim(),
            Type = request.Type!.Value,
            InitialBalance = request.InitialBalance!.Value
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = account.Id },
            AccountResponseDto.FromModel(account));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountResponseDto>> Update(
        Guid id, AccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryValidateRequest(request, out var error)) return BadRequest(new { error });

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);

        if (account is null) return NotFound();

        account.Name = request.Name!.Trim();
        account.Type = request.Type!.Value;
        account.InitialBalance = request.InitialBalance!.Value;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }

        return Ok(AccountResponseDto.FromModel(account));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);

        if (account is null) return NotFound();

        // A FK existente em FinanceTransaction impede apagar contas com movimentos.
        if (await db.Transactions.AnyAsync(
            t => t.UserId == userId && t.AccountId == id, cancellationToken))
        {
            return Conflict(new { error =
                "Nao e possivel eliminar uma conta com transacoes associadas." });
        }

        db.Accounts.Remove(account);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // Protege tambem contra uma transacao criada entre a verificacao e o DELETE.
            return Conflict(new { error =
                "Nao e possivel eliminar uma conta com transacoes associadas." });
        }

        return NoContent();
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

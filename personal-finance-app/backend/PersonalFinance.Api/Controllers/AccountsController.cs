using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Accounts;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountsController(IAccountService accounts) : ControllerBase
{
    public AccountsController(AppDbContext db) : this(new AccountService(db)) { }

    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await accounts.GetAllAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var account = await accounts.GetByIdAsync(userId, id, cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create(
        AccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await accounts.CreateAsync(userId, request, cancellationToken);
        if (result.InvalidRequest) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Account!.Id }, result.Account);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountResponseDto>> Update(
        Guid id, AccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await accounts.UpdateAsync(userId, id, request, cancellationToken);
        if (result.InvalidRequest) return BadRequest(new { error = result.Error });
        return result.Account is null ? NotFound() : Ok(result.Account);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await accounts.DeleteAsync(userId, id, cancellationToken) switch
        {
            AccountDeleteResult.Deleted => NoContent(),
            AccountDeleteResult.HasTransactions => Conflict(new { error =
                "Nao e possivel eliminar uma conta com transacoes associadas." }),
            _ => NotFound()
        };
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Transactions;
using PersonalFinance.Api.Models;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(ITransactionService transactions) : ControllerBase
{
    public TransactionsController(AppDbContext db) : this(new TransactionService(db)) { }

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetAll(
        [FromQuery(Name = "from")] DateOnly? from, [FromQuery(Name = "to")] DateOnly? to,
        [FromQuery] TransactionType? type, [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (from > to) return BadRequest(new { error = "A data inicial deve ser anterior ou igual a data final." });
        if (type.HasValue && !Enum.IsDefined(type.Value))
            return BadRequest(new { error = "O tipo deve ser Income ou Expense." });
        return Ok(await transactions.GetAllAsync(userId, from, to, type, accountId, categoryId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var transaction = await transactions.GetByIdAsync(userId, id, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> Create(
        TransactionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await transactions.CreateAsync(userId, request, cancellationToken);
        if (result.InvalidRequest) return BadRequest(new { error = result.Error });
        if (result.Transaction is null) return NotFound(new { error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Transaction.Id }, result.Transaction);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionResponseDto>> Update(
        Guid id, TransactionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await transactions.UpdateAsync(userId, id, request, cancellationToken);
        if (result.InvalidRequest) return BadRequest(new { error = result.Error });
        if (result.Transaction is null)
            return result.Error is null ? NotFound() : NotFound(new { error = result.Error });
        return Ok(result.Transaction);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await transactions.DeleteAsync(userId, id, cancellationToken)
            ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

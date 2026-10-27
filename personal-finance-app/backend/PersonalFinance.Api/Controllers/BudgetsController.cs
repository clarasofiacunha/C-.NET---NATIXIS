using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.DTOs.Budgets;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/budgets")]
public sealed class BudgetsController(IBudgetService budgets) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List monthly budgets")]
    [EndpointDescription("Returns the authenticated user's budgets, optionally filtered by year and month.")]
    [ProducesResponseType<IReadOnlyList<BudgetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<BudgetResponse>>> GetAll(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12)
            return BadRequest(new { error = "Ano ou mês inválido." });

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await budgets.GetAllAsync(userId, year, month, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a monthly budget")]
    [EndpointDescription("Returns one budget owned by the authenticated user.")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BudgetResponse>> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var budget = await budgets.GetByIdAsync(userId, id, cancellationToken);
        return budget is null ? NotFound() : Ok(budget);
    }

    [HttpPost]
    [EndpointSummary("Create a monthly budget")]
    [EndpointDescription("Creates a budget for one of the authenticated user's expense categories.")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BudgetResponse>> Create(
        CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await budgets.CreateAsync(userId, request, cancellationToken);
        return result.Status switch
        {
            BudgetOperationStatus.Success =>
                CreatedAtAction(nameof(GetById), new { id = result.Budget!.Id }, result.Budget),
            BudgetOperationStatus.NotFound =>
                NotFound(new { error = "Categoria de despesa não encontrada." }),
            BudgetOperationStatus.Duplicate =>
                Conflict(new { error = "Já existe um orçamento para esta categoria neste mês." }),
            _ => throw new InvalidOperationException("Estado de operação de orçamento desconhecido.")
        };
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Replace a monthly budget")]
    [EndpointDescription("Replaces the category, month, year, and amount of an owned budget.")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BudgetResponse>> Update(
        Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await budgets.UpdateAsync(userId, id, request, cancellationToken);
        return result.Status switch
        {
            BudgetOperationStatus.Success => Ok(result.Budget),
            BudgetOperationStatus.NotFound => NotFound(new
            {
                error = "Orçamento ou categoria de despesa não encontrada."
            }),
            BudgetOperationStatus.Duplicate =>
                Conflict(new { error = "Já existe um orçamento para esta categoria neste mês." }),
            _ => throw new InvalidOperationException("Estado de operação de orçamento desconhecido.")
        };
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a monthly budget")]
    [EndpointDescription("Deletes one budget owned by the authenticated user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return await budgets.DeleteAsync(userId, id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}

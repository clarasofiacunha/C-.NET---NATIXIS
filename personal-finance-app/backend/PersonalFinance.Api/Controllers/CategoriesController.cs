using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.DTOs.Categories;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Listar categorias")]
    [EndpointDescription("Devolve as categorias do utilizador autenticado, ordenadas por tipo e nome.")]
    [ProducesResponseType<IReadOnlyList<CategoryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await categories.GetAllAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obter uma categoria")]
    [EndpointDescription("Devolve uma categoria pertencente ao utilizador autenticado.")]
    [ProducesResponseType<CategoryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponseDto>> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var category = await categories.GetByIdAsync(userId, id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [EndpointSummary("Criar categoria")]
    [EndpointDescription("Cria uma categoria de receita ou despesa para o utilizador autenticado.")]
    [ProducesResponseType<CategoryResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponseDto>> Create(
        CategoryRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!TryValidateRequest(request, out var error))
            return BadRequest(new { error });

        var result = await categories.CreateAsync(userId, request, cancellationToken);
        if (result.Status == CategoryOperationStatus.Duplicate)
            return Conflict(new { error = "Já existe uma categoria com este nome e tipo." });

        return CreatedAtAction(
            nameof(GetById), new { id = result.Category!.Id }, result.Category);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Atualizar categoria")]
    [EndpointDescription("Substitui o nome e o tipo de uma categoria pertencente ao utilizador.")]
    [ProducesResponseType<CategoryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponseDto>> Update(
        Guid id, CategoryRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!TryValidateRequest(request, out var error))
            return BadRequest(new { error });

        var result = await categories.UpdateAsync(userId, id, request, cancellationToken);
        return result.Status switch
        {
            CategoryOperationStatus.Success => Ok(result.Category),
            CategoryOperationStatus.NotFound => NotFound(),
            CategoryOperationStatus.Duplicate =>
                Conflict(new { error = "Já existe uma categoria com este nome e tipo." }),
            CategoryOperationStatus.InUse => Conflict(new
            {
                error = "Não é possível alterar o tipo de uma categoria já utilizada."
            }),
            _ => throw new InvalidOperationException("Estado de operação de categoria desconhecido.")
        };
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Eliminar categoria")]
    [EndpointDescription("Elimina uma categoria sem transações ou orçamentos associados.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await categories.DeleteAsync(userId, id, cancellationToken);
        return result switch
        {
            CategoryDeleteStatus.Success => NoContent(),
            CategoryDeleteStatus.NotFound => NotFound(),
            CategoryDeleteStatus.InUse => Conflict(new
            {
                error = "Não é possível eliminar uma categoria com transações ou orçamentos associados."
            }),
            _ => throw new InvalidOperationException("Estado de eliminação de categoria desconhecido.")
        };
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static bool TryValidateRequest(CategoryRequestDto request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "O nome da categoria não pode estar vazio.";
            return false;
        }

        if (request.Name.Trim().Length > 100)
        {
            error = "O nome pode ter, no máximo, 100 caracteres.";
            return false;
        }

        if (request.Type is null || !Enum.IsDefined(request.Type.Value))
        {
            error = "O tipo deve ser Income ou Expense.";
            return false;
        }

        error = null;
        return true;
    }
}

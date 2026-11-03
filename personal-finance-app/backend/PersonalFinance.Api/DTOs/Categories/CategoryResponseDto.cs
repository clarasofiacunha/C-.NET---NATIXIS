using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Categories;

/// <summary>Categoria pertencente ao utilizador autenticado.</summary>
public sealed record CategoryResponseDto(Guid Id, string Name, TransactionType Type);

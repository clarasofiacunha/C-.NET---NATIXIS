using System.ComponentModel.DataAnnotations;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Categories;

/// <summary>Dados para criar ou atualizar uma categoria.</summary>
public sealed record CategoryRequestDto
{
    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome pode ter, no máximo, 100 caracteres.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "O tipo da categoria é obrigatório.")]
    public TransactionType? Type { get; init; }
}

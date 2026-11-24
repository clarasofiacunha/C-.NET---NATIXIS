using System.ComponentModel.DataAnnotations;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Categories;

/// <summary>Dados para criar ou atualizar uma categoria.</summary>
public sealed record CategoryRequestDto
{
    [Required(ErrorMessage = "The category name is required.")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "The category type is required.")]
    public TransactionType? Type { get; init; }
}

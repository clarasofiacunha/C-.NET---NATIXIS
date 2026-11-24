using System.ComponentModel.DataAnnotations;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Accounts;

// O mesmo contrato e utilizado na criacao (POST) e na edicao completa (PUT).
public sealed class AccountRequestDto
{
    [Required(ErrorMessage = "The name is required.")]
    [StringLength(120, ErrorMessage = "The name can have at most 120 characters.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "The account type is required.")]
    public AccountType? Type { get; set; }

    [Required(ErrorMessage = "The initial balance is required.")]
    [Range(typeof(decimal), "-9999999999999999.99", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "The initial balance is outside the allowed range.")]
    public decimal? InitialBalance { get; set; }
}

using System.ComponentModel.DataAnnotations;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Accounts;

// O mesmo contrato e utilizado na criacao (POST) e na edicao completa (PUT).
public sealed class AccountRequestDto
{
    [Required(ErrorMessage = "O nome e obrigatorio.")]
    [StringLength(120, ErrorMessage = "O nome pode ter, no maximo, 120 caracteres.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "O tipo de conta e obrigatorio.")]
    public AccountType? Type { get; set; }

    [Required(ErrorMessage = "O saldo inicial e obrigatorio.")]
    [Range(typeof(decimal), "-9999999999999999.99", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "O saldo inicial esta fora do intervalo permitido.")]
    public decimal? InitialBalance { get; set; }
}

using System.ComponentModel.DataAnnotations;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Transactions;

// POST e PUT usam o mesmo contrato. O UserId vem sempre da sessao autenticada.
public sealed class TransactionRequestDto
{
    [Required(ErrorMessage = "The account is required.")]
    public Guid? AccountId { get; set; }

    [Required(ErrorMessage = "The category is required.")]
    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = "The amount is required.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "The amount must be positive and within the allowed limit.")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "The type is required.")]
    public TransactionType? Type { get; set; }

    // DateTimeOffset permite receber um instante com offset e gravar sempre em UTC.
    [Required(ErrorMessage = "The date is required.")]
    public DateTimeOffset? Date { get; set; }

    [StringLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

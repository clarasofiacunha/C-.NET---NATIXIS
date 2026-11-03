using System.ComponentModel.DataAnnotations;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Transactions;

// POST e PUT usam o mesmo contrato. O UserId vem sempre da sessao autenticada.
public sealed class TransactionRequestDto
{
    [Required(ErrorMessage = "A conta e obrigatoria.")]
    public Guid? AccountId { get; set; }

    [Required(ErrorMessage = "A categoria e obrigatoria.")]
    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = "O montante e obrigatorio.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "O montante deve ser positivo e estar dentro do limite permitido.")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "O tipo e obrigatorio.")]
    public TransactionType? Type { get; set; }

    // DateTimeOffset permite receber um instante com offset e gravar sempre em UTC.
    [Required(ErrorMessage = "A data e obrigatoria.")]
    public DateTimeOffset? Date { get; set; }

    [StringLength(500, ErrorMessage = "A descricao nao pode exceder 500 caracteres.")]
    public string? Description { get; set; }
}

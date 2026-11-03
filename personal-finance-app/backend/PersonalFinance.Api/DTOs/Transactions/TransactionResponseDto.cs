using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Transactions;

public sealed record TransactionResponseDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    string? Description);

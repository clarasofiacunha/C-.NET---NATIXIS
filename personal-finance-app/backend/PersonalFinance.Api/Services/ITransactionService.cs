using PersonalFinance.Api.DTOs.Transactions;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Services;

public interface ITransactionService
{
    Task<List<TransactionResponseDto>> GetAllAsync(Guid userId, DateOnly? from, DateOnly? to,
        TransactionType? type, Guid? accountId, Guid? categoryId, CancellationToken cancellationToken);
    Task<TransactionResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<TransactionOperationResult> CreateAsync(Guid userId, TransactionRequestDto request, CancellationToken cancellationToken);
    Task<TransactionOperationResult> UpdateAsync(Guid userId, Guid id, TransactionRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}

public sealed record TransactionOperationResult(TransactionResponseDto? Transaction, string? Error, bool InvalidRequest = false)
{
    public bool Succeeded => Transaction is not null;
}
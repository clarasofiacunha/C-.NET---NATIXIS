using PersonalFinance.Api.DTOs;
using PersonalFinance.Api.DTOs.Transactions;

namespace PersonalFinance.Api.Services;

public interface ITransactionService
{
    Task<PagedResponse<TransactionResponseDto>> GetAllAsync(Guid userId, TransactionQueryParameters query,
        CancellationToken cancellationToken);
    Task<TransactionResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<TransactionOperationResult> CreateAsync(Guid userId, TransactionRequestDto request, CancellationToken cancellationToken);
    Task<TransactionOperationResult> UpdateAsync(Guid userId, Guid id, TransactionRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}

public sealed record TransactionOperationResult(TransactionResponseDto? Transaction, string? Error, bool InvalidRequest = false)
{
    public bool Succeeded => Transaction is not null;
}
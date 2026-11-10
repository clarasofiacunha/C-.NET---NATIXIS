using PersonalFinance.Api.DTOs.Accounts;

namespace PersonalFinance.Api.Services;

public interface IAccountService
{
    Task<List<AccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<AccountOperationResult> CreateAsync(Guid userId, AccountRequestDto request, CancellationToken cancellationToken);
    Task<AccountOperationResult> UpdateAsync(Guid userId, Guid id, AccountRequestDto request, CancellationToken cancellationToken);
    Task<AccountDeleteResult> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}

public sealed record AccountOperationResult(AccountResponseDto? Account, string? Error, bool InvalidRequest = false)
{
    public bool Succeeded => Account is not null;
}

public enum AccountDeleteResult
{
    Deleted,
    NotFound,
    HasTransactions
}
using PersonalFinance.Api.DTOs.Categories;

namespace PersonalFinance.Api.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<CategoryResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<CategoryOperationResult> CreateAsync(
        Guid userId, CategoryRequestDto request, CancellationToken cancellationToken);
    Task<CategoryOperationResult> UpdateAsync(
        Guid userId, Guid id, CategoryRequestDto request, CancellationToken cancellationToken);
    Task<CategoryDeleteStatus> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}

public enum CategoryOperationStatus
{
    Success,
    NotFound,
    Duplicate,
    InUse
}

public sealed record CategoryOperationResult(
    CategoryOperationStatus Status,
    CategoryResponseDto? Category = null);

public enum CategoryDeleteStatus
{
    Success,
    NotFound,
    InUse
}

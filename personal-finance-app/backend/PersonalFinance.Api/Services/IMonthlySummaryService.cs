using PersonalFinance.Api.DTOs.Summary;

namespace PersonalFinance.Api.Services;

public interface IMonthlySummaryService
{
    Task<MonthlySummaryResponse> GetAsync(
        Guid userId, int year, int month, CancellationToken cancellationToken);
}

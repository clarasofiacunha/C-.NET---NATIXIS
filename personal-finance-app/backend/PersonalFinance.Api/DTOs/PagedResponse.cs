namespace PersonalFinance.Api.DTOs;

// Envelope generico para respostas paginadas.
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

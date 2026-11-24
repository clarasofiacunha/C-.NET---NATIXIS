using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.DTOs;
using PersonalFinance.Api.DTOs.Transactions;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Data;

// Logica partilhada de filtros, ordenacao e paginacao das transacoes.
public static class TransactionQueryExtensions
{
    public static IQueryable<FinanceTransaction> ApplyFilters(
        this IQueryable<FinanceTransaction> query, TransactionQueryParameters p)
    {
        // Intervalo [00:00 UTC do inicio, 00:00 UTC do dia seguinte ao fim).
        // Isto inclui todas as horas do ultimo dia e permite usar o indice por data.
        if (p.From.HasValue)
        {
            var start = p.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.Date >= start);
        }
        if (p.To.HasValue && p.To.Value < DateOnly.MaxValue)
        {
            var endExclusive = p.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.Date < endExclusive);
        }
        if (p.Type.HasValue) query = query.Where(t => t.Type == p.Type.Value);
        if (p.AccountId.HasValue) query = query.Where(t => t.AccountId == p.AccountId.Value);
        if (p.AccountType.HasValue) query = query.Where(t => t.Account.Type == p.AccountType.Value);
        if (p.CategoryId.HasValue) query = query.Where(t => t.CategoryId == p.CategoryId.Value);
        if (p.MinAmount.HasValue) query = query.Where(t => t.Amount >= p.MinAmount.Value);
        if (p.MaxAmount.HasValue) query = query.Where(t => t.Amount <= p.MaxAmount.Value);
        return query;
    }

    public static IQueryable<FinanceTransaction> ApplySorting(
        this IQueryable<FinanceTransaction> query, TransactionQueryParameters p)
    {
        var desc = p.SortDirection == SortDirection.Desc;
        var ordered = p.SortBy switch
        {
            TransactionSortField.Amount => desc
                ? query.OrderByDescending(t => t.Amount)
                : query.OrderBy(t => t.Amount),
            TransactionSortField.Description => desc
                ? query.OrderByDescending(t => t.Description)
                : query.OrderBy(t => t.Description),
            _ => desc
                ? query.OrderByDescending(t => t.Date)
                : query.OrderBy(t => t.Date)
        };
        // Desempates estaveis: sem eles, a mesma linha pode aparecer em duas paginas.
        if (p.SortBy != TransactionSortField.Date)
            ordered = desc ? ordered.ThenByDescending(t => t.Date) : ordered.ThenBy(t => t.Date);
        return ordered.ThenBy(t => t.Id);
    }

    public static async Task<PagedResponse<TransactionResponseDto>> ToPagedResponseAsync(
        this IQueryable<FinanceTransaction> filtered, TransactionQueryParameters p,
        CancellationToken cancellationToken)
    {
        var totalCount = await filtered.CountAsync(cancellationToken);
        var skip = (long)(p.Page - 1) * p.PageSize;

        var items = skip >= totalCount
            ? []
            : await filtered
                .ApplySorting(p)
                .Skip((int)skip)
                .Take(p.PageSize)
                .Select(t => new TransactionResponseDto(
                    t.Id, t.AccountId, t.Account.Name, t.CategoryId, t.Category.Name,
                    t.Amount, t.Type, t.Date, t.Description))
                .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)p.PageSize);
        return new PagedResponse<TransactionResponseDto>(items, p.Page, p.PageSize, totalCount, totalPages);
    }
}

using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Transactions;

// Parametros de GET /api/transactions (query string).
// Ex.: ?categoryId=...&accountId=...&from=2026-10-01&to=2026-10-31&minAmount=10&sortBy=Amount&sortDirection=Desc&page=2
public sealed class TransactionQueryParameters
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxPage = 1_000_000;

    // Filtros
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public TransactionType? Type { get; set; }
    public Guid? AccountId { get; set; }
    // Todas as contas de um tipo (ex.: Savings), com os mesmos valores do AccountType.
    public AccountType? AccountType { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    // Ordenacao (por omissao: mais recente primeiro)
    public TransactionSortField SortBy { get; set; } = TransactionSortField.Date;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    // Paginacao (page comeca em 1)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    public string? Validate()
    {
        if (From > To) return "The start date must be on or before the end date.";
        if (Type.HasValue && !Enum.IsDefined(Type.Value)) return "The type must be Income or Expense.";
        if (AccountType.HasValue && !Enum.IsDefined(AccountType.Value)) return "The selected account type is not valid.";
        if (MinAmount < 0m || MaxAmount < 0m) return "The minimum and maximum amounts cannot be negative.";
        if (MinAmount > MaxAmount) return "The minimum amount must be less than or equal to the maximum amount.";
        if (!Enum.IsDefined(SortBy)) return "sortBy must be Date, Amount or Description.";
        if (!Enum.IsDefined(SortDirection)) return "sortDirection must be Asc or Desc.";
        if (Page < 1 || Page > MaxPage) return $"page must be between 1 and {MaxPage}.";
        if (PageSize < 1 || PageSize > MaxPageSize) return $"pageSize must be between 1 and {MaxPageSize}.";
        return null;
    }
}

public enum TransactionSortField
{
    Date,
    Amount,
    Description
}

public enum SortDirection
{
    Asc,
    Desc
}

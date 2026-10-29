using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.DTOs.Budgets;

/// <summary>Payload for creating a monthly budget for an expense category.</summary>
public sealed record CreateBudgetRequest
{
    /// <summary>Category to which this budget applies.</summary>
    [NotEmptyGuid]
    public required Guid CategoryId { get; init; }

    /// <summary>Month of the budget, from 1 to 12.</summary>
    [Range(1, 12)]
    public required int Month { get; init; }

    /// <summary>Calendar year of the budget.</summary>
    [Range(1, 9999)]
    public required int Year { get; init; }

    /// <summary>Positive spending limit for the category.</summary>
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
    ParseLimitsInInvariantCulture = true,
    ConvertValueInInvariantCulture = true)]
    [TwoDecimalPlaces]
    public required decimal LimitAmount { get; init; }
}

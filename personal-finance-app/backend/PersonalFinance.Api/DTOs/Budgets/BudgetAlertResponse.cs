using System.Text.Json.Serialization;

namespace PersonalFinance.Api.DTOs.Budgets;

/// <summary>Current consumption alert for one monthly category budget.</summary>
public sealed record BudgetAlertResponse(
    Guid BudgetId,
    Guid CategoryId,
    string CategoryName,
    int Month,
    int Year,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal UsagePercentage,
    BudgetAlertStatus Status);

/// <summary>Indicates whether spending is approaching or beyond a budget limit.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BudgetAlertStatus
{
    NearLimit,
    Exceeded
}

/// <summary>Classifies spending against the monthly budget alert thresholds.</summary>
public static class BudgetAlertClassifier
{
    public const decimal NearLimitThreshold = 0.80m;

    /// <summary>Returns the active alert status, or null when spending is below the alert threshold.</summary>
    public static BudgetAlertStatus? Classify(decimal limitAmount, decimal spentAmount)
    {
        if (limitAmount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(limitAmount), "The budget limit must be positive.");

        if (spentAmount < 0m)
            throw new ArgumentOutOfRangeException(nameof(spentAmount), "Spent amount cannot be negative.");

        if (spentAmount > limitAmount)
            return BudgetAlertStatus.Exceeded;

        return spentAmount >= limitAmount * NearLimitThreshold
            ? BudgetAlertStatus.NearLimit
            : null;
    }
}

using PersonalFinance.Api.DTOs.Budgets;

namespace PersonalFinance.Tests;

public sealed class BudgetAlertClassifierTests
{
    [Theory]
    [InlineData(0d)]
    [InlineData(79.99d)]
    public void Classify_ReturnsNoAlertBelowThreshold(double percent)
    {
        var status = BudgetAlertClassifier.Classify(100m, (decimal)percent);

        Assert.Null(status);
    }

    [Theory]
    [InlineData(80d)]
    [InlineData(99.99d)]
    [InlineData(100d)]
    public void Classify_ReturnsNearLimitFrom80Through100Percent(double percent)
    {
        var status = BudgetAlertClassifier.Classify(100m, (decimal)percent);

        Assert.Equal(BudgetAlertStatus.NearLimit, status);
    }

    [Theory]
    [InlineData(100.01d)]
    [InlineData(125d)]
    public void Classify_ReturnsExceededAbove100Percent(double spentAmount)
    {
        var status = BudgetAlertClassifier.Classify(100m, (decimal)spentAmount);

        Assert.Equal(BudgetAlertStatus.Exceeded, status);
    }

    [Fact]
    public void Classify_RejectsNonPositiveLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BudgetAlertClassifier.Classify(0m, 0m));
    }

    [Fact]
    public void Classify_RejectsNegativeSpentAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BudgetAlertClassifier.Classify(100m, -1m));
    }
}

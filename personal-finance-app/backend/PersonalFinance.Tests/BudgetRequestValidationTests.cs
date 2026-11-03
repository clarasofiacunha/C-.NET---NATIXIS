using System.ComponentModel.DataAnnotations;
using PersonalFinance.Api.DTOs.Budgets;

namespace PersonalFinance.Tests;

public sealed class BudgetRequestValidationTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void CreateRequest_AcceptsValidBudget()
    {
        var request = new CreateBudgetRequest
        {
            CategoryId = CategoryId,
            Month = 10,
            Year = 2026,
            LimitAmount = 250.50m
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void UpdateRequest_AcceptsValidBudget()
    {
        var request = new UpdateBudgetRequest
        {
            CategoryId = CategoryId,
            Month = 10,
            Year = 2026,
            LimitAmount = 250.50m
        };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void CreateRequest_RejectsMonthOutsideCalendar(int month)
    {
        var request = ValidCreateRequest() with { Month = month };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Month)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void UpdateRequest_RejectsMonthOutsideCalendar(int month)
    {
        var request = ValidUpdateRequest() with { Month = month };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Month)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void CreateRequest_RejectsYearOutsideSupportedRange(int year)
    {
        var request = ValidCreateRequest() with { Year = year };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Year)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void UpdateRequest_RejectsYearOutsideSupportedRange(int year)
    {
        var request = ValidUpdateRequest() with { Year = year };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Year)));
    }

    [Fact]
    public void CreateRequest_RejectsAmountWithMoreThanTwoDecimalPlaces()
    {
        var request = ValidCreateRequest() with { LimitAmount = 100.001m };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.LimitAmount)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000000000000000d)]
    public void CreateRequest_RejectsNonPositiveOrOutOfRangeAmount(double amount)
    {
        var request = ValidCreateRequest() with { LimitAmount = (decimal)amount };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.LimitAmount)));
    }

    [Fact]
    public void UpdateRequest_RejectsAmountWithMoreThanTwoDecimalPlaces()
    {
        var request = ValidUpdateRequest() with { LimitAmount = 100.001m };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.LimitAmount)));
    }

    [Fact]
    public void CreateRequest_RejectsEmptyCategoryId()
    {
        var request = ValidCreateRequest() with { CategoryId = Guid.Empty };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.CategoryId)));
    }

    [Fact]
    public void UpdateRequest_RejectsEmptyCategoryId()
    {
        var request = ValidUpdateRequest() with { CategoryId = Guid.Empty };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.CategoryId)));
    }

    private static CreateBudgetRequest ValidCreateRequest() => new()
    {
        CategoryId = CategoryId,
        Month = 10,
        Year = 2026,
        LimitAmount = 250.50m
    };

    private static UpdateBudgetRequest ValidUpdateRequest() => new()
    {
        CategoryId = CategoryId,
        Month = 10,
        Year = 2026,
        LimitAmount = 250.50m
    };

    private static IReadOnlyList<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);
        return results;
    }
}

using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.DTOs.Budgets;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is Guid id && id != Guid.Empty;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class TwoDecimalPlacesAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is decimal amount && amount == decimal.Round(amount, 2);
    }
}

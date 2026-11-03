using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.DTOs.Accounts;

// Nao expoe a entidade EF nem os dados de outros utilizadores.
public sealed record AccountResponseDto(
    Guid Id,
    string Name,
    AccountType Type,
    decimal InitialBalance,
    DateTime CreatedAt)
{
    public static AccountResponseDto FromModel(Account account) => new(
        account.Id,
        account.Name,
        account.Type,
        account.InitialBalance,
        account.CreatedAt);
}

namespace PersonalFinance.Api.DTOs.Auth;

public sealed record LoginCheckRequestDto(string? Email, string? Password);

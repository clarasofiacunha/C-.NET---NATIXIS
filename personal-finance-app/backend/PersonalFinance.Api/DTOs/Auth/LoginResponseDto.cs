namespace PersonalFinance.Api.DTOs.Auth;

public sealed record LoginResponseDto(Guid Id, string Name, string? Email);
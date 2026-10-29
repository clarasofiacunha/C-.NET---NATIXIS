namespace PersonalFinance.Api.DTOs.Auth;

public sealed record RegisterResponseDto(Guid Id, string Name, string? Email);
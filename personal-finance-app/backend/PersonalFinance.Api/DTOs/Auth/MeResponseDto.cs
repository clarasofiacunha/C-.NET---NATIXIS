namespace PersonalFinance.Api.DTOs.Auth;

public sealed record MeResponseDto(Guid Id, string Name, string? Email);
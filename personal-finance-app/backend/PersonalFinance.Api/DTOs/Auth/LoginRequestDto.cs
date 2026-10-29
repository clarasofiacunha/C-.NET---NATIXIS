using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.DTOs.Auth;

public sealed record LoginRequestDto(
    [param: Required, EmailAddress, StringLength(256)] string? Email,
    [param: Required, StringLength(100, MinimumLength = 8)] string? Password);

using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.DTOs.Auth;

public sealed record RegisterRequestDto(
	[param: Required, StringLength(150, MinimumLength = 2)] string? Name,
	[param: Required, EmailAddress, StringLength(256)] string? Email,
	[param: Required, StringLength(100, MinimumLength = 8)] string? Password);

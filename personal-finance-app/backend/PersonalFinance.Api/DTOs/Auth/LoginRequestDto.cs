using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.DTOs.Auth;

public sealed record LoginRequestDto(
	[param: Required, EmailAddress, StringLength(256)] string? Email,
	[param: Required] string? Password);

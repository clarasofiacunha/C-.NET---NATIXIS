using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.DTOs.Auth;

public sealed record ResetPasswordRequestDto(
    [param: Required, EmailAddress, StringLength(256)] string? Email,
    [param: Required] string? Token,
    [param: Required, StringLength(100)] string? NewPassword);

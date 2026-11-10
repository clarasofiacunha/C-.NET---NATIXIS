using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.DTOs.Auth;

public sealed record ForgotPasswordRequestDto(
    [param: Required, EmailAddress, StringLength(256)] string? Email);

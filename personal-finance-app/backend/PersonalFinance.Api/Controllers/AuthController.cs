using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PersonalFinance.Api.DTOs.Auth;
using PersonalFinance.Api.Models;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IRecoveryEmailSender recoveryEmailSender,
    IOptions<BrevoOptions> brevoOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    private const string PasswordRecoveryMessage =
        "Se existir uma conta com este email, receberá instruções para redefinir a palavra-passe.";

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var email = request.Email!.Trim();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };

        var result = await userManager.CreateAsync(user, request.Password!);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray())
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new RegisterResponseDto(user.Id, user.Name, user.Email));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var email = request.Email!.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
            return Unauthorized(new { error = "Email ou password inválidos." });

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password!,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
            return Unauthorized(new { error = "Email ou password inválidos." });

        return Ok(new LoginResponseDto(user.Id, user.Name, user.Email));
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("password-recovery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var email = request.Email!.Trim();
        var user = await userManager.FindByEmailAsync(email);

        // Keep the response identical whether the account exists or not.
        if (user is null)
            return Ok(new { message = PasswordRecoveryMessage });

        try
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = CreateResetLink(brevoOptions.Value.FrontendBaseUrl, email, token);
            await recoveryEmailSender.SendPasswordResetAsync(email, resetLink, cancellationToken);
        }
        catch (RecoveryEmailDeliveryException exception)
        {
            // Do not include the email address, token, API key, or reset link in logs.
            logger.LogWarning(exception, "Could not deliver a password recovery email.");
        }
        catch (InvalidOperationException exception)
        {
            // A bad server configuration must not expose account existence to callers.
            logger.LogError(exception, "Password recovery email configuration is invalid.");
        }

        return Ok(new { message = PasswordRecoveryMessage });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        var user = await userManager.FindByEmailAsync(request.Email!.Trim());
        if (user is null)
            return BadRequest(new { error = "O link de recuperação é inválido ou expirou." });

        var result = await userManager.ResetPasswordAsync(user, request.Token!, request.NewPassword!);
        if (result.Succeeded)
            return NoContent();

        var tokenError = result.Errors.Any(error => error.Code == "InvalidToken");
        if (tokenError)
            return BadRequest(new { error = "O link de recuperação é inválido ou expirou." });

        return BadRequest(new
        {
            errors = result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray())
        });
    }

    private static Uri CreateResetLink(string? frontendBaseUrl, string email, string token)
    {
        if (!Uri.TryCreate(frontendBaseUrl, UriKind.Absolute, out var frontendUri) ||
            (frontendUri.Scheme != Uri.UriSchemeHttp && frontendUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("The password recovery frontend URL is not configured correctly.");
        }

        var link = new UriBuilder(frontendUri)
        {
            Fragment = $"reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}"
        };

        return link.Uri;
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return Ok(new MeResponseDto(user.Id, user.Name, user.Email));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.DTOs.Auth;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(UserManager<AppUser> userManager) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var email = request.Email!.Trim();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            UserName = email,
            Email = email
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

        return StatusCode(StatusCodes.Status201Created, new
        {
            user.Id,
            user.Name,
            user.Email
        });
    }
}
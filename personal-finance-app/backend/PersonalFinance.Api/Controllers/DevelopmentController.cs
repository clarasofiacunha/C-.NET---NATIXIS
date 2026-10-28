using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.DTOs.Auth;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("dev")]
public class DevelopmentController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IWebHostEnvironment environment) : ControllerBase
{
    private bool IsDevelopment => environment.IsDevelopment();

    [HttpGet("db")]
    public async Task<IActionResult> GetDatabaseCounts()
    {
        if (!IsDevelopment) return NotFound();

        return Ok(new
        {
            users = await db.Users.CountAsync(),
            accounts = await db.Accounts.CountAsync(),
            categories = await db.Categories.CountAsync(),
            transactions = await db.Transactions.CountAsync(),
            budgets = await db.Budgets.CountAsync(),
            goals = await db.Goals.CountAsync()
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!IsDevelopment) return NotFound();

        return Ok(await db.Users.AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, u.Email, u.CreatedAt })
            .ToListAsync());
    }

    [HttpPost("auth/check")]
    public async Task<IActionResult> CheckPassword(LoginCheckRequestDto request)
    {
        if (!IsDevelopment) return NotFound();

        var user = await userManager.FindByEmailAsync(request.Email ?? "");
        var valid = user is not null
            && await userManager.CheckPasswordAsync(user, request.Password ?? "");

        return Ok(new { passwordCorrect = valid });
    }

    // Login temporario: apenas em Development, ate existir o AuthController definitivo.
    // Usa a password real do ASP.NET Identity; nao aceita identificadores arbitrarios de utilizador.
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(LoginCheckRequestDto request)
    {
        if (!IsDevelopment) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email e palavra-passe sao obrigatorios." });

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return Unauthorized(new { error = "Credenciais invalidas." });

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
            return Unauthorized(new { error = "Credenciais invalidas ou acesso bloqueado." });

        return Ok(new { user.Id, user.Name, user.Email });
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        if (!IsDevelopment) return NotFound();
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("users/{userId:guid}/accounts")]
    public async Task<IActionResult> GetAccounts(Guid userId)
    {
        if (!IsDevelopment) return NotFound();

        return Ok(await db.Accounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .Select(a => new { a.Id, a.Name, a.Type, a.InitialBalance, a.CreatedAt })
            .ToListAsync());
    }

    [HttpGet("users/{userId:guid}/categories")]
    public async Task<IActionResult> GetCategories(Guid userId)
    {
        if (!IsDevelopment) return NotFound();

        return Ok(await db.Categories.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Type })
            .ToListAsync());
    }

    [HttpGet("users/{userId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid userId)
    {
        if (!IsDevelopment) return NotFound();

        return Ok(await db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Id)
            .Take(50)
            .Select(t => new
            {
                t.Id, t.Date, t.Type, t.Amount, t.Description,
                t.AccountId,
                account = t.Account.Name,
                t.CategoryId,
                category = t.Category.Name
            })
            .ToListAsync());
    }

    [HttpGet("users/{userId:guid}/budgets")]
    public async Task<IActionResult> GetBudgets(Guid userId)
    {
        if (!IsDevelopment) return NotFound();

        return Ok(await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .Select(b => new
            {
                b.Id, b.Year, b.Month, b.LimitAmount,
                b.CategoryId,
                category = b.Category.Name
            })
            .ToListAsync());
    }

    [HttpGet("users/{userId:guid}/goals")]
    public async Task<IActionResult> GetGoals(Guid userId)
    {
        if (!IsDevelopment) return NotFound();

        return Ok(await db.Goals.AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Deadline)
            .Select(g => new { g.Id, g.Name, g.TargetAmount, g.CurrentAmount, g.Deadline })
            .ToListAsync());
    }

    [HttpGet("users/{userId:guid}/dashboard")]
    public async Task<IActionResult> GetDashboard(Guid userId, int? year, int? month)
    {
        if (!IsDevelopment) return NotFound();

        var today = DateTime.UtcNow;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;
        if (selectedYear is < 1 or > 9998 || selectedMonth is < 1 or > 12)
            return BadRequest(new { error = "Ano ou mes invalido." });

        if (!await db.Users.AnyAsync(u => u.Id == userId))
            return NotFound(new { error = "Utilizador nao encontrado." });

        var start = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var monthTransactions = db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end);

        var income = await monthTransactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        var expense = await monthTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        var budgetLimit = await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == selectedYear && b.Month == selectedMonth)
            .SumAsync(b => (decimal?)b.LimitAmount) ?? 0m;

        return Ok(new
        {
            year = selectedYear,
            month = selectedMonth,
            income,
            expense,
            balanceForMonth = income - expense,
            totalBudgetLimit = budgetLimit
        });
    }
}

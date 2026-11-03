using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Controllers;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Summary;
using PersonalFinance.Api.Models;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Tests;

public sealed class MonthlySummaryControllerTests
{
    private static AppDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"summary-test-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static SummaryController NewController(AppDbContext db, Guid? userId)
    {
        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : Array.Empty<Claim>();
        return new SummaryController(new MonthlySummaryService(db))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims,
                        userId.HasValue ? "TestAuth" : null))
                }
            }
        };
    }

    private static (Account Account, Category Food, Category Travel, Category Leisure, Category Salary)
        SeedUser(AppDbContext db, Guid userId, decimal openingBalance = 1000m)
    {
        db.Users.Add(new AppUser
        {
            Id = userId,
            Name = "Utilizador",
            UserName = $"user-{userId:N}@example.test",
            Email = $"user-{userId:N}@example.test"
        });
        var account = new Account
        {
            UserId = userId, Name = "Conta à ordem", InitialBalance = openingBalance,
            Type = AccountType.Checking
        };
        var food = new Category { UserId = userId, Name = "Alimentação", Type = TransactionType.Expense };
        var travel = new Category { UserId = userId, Name = "Transportes", Type = TransactionType.Expense };
        var leisure = new Category { UserId = userId, Name = "Lazer", Type = TransactionType.Expense };
        var salary = new Category { UserId = userId, Name = "Salário", Type = TransactionType.Income };
        db.Accounts.Add(account);
        db.Categories.AddRange(food, travel, leisure, salary);
        db.SaveChanges();
        return (account, food, travel, leisure, salary);
    }

    private static FinanceTransaction Transaction(Guid userId, Account account,
        Category category, decimal amount, DateTime date) => new()
    {
        UserId = userId,
        AccountId = account.Id,
        CategoryId = category.Id,
        Amount = amount,
        Type = category.Type,
        Date = date
    };

    private static MonthlySummaryResponse GetResponse(ActionResult<MonthlySummaryResponse> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<MonthlySummaryResponse>(ok.Value);
    }

    [Fact]
    public async Task GetMonthly_CalculatesBalanceExpensesAndBudgets_WithoutLeakingOtherUsers()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, food, travel, leisure, salary) = SeedUser(db, userId);
        db.Accounts.Add(new Account
        {
            UserId = userId, Name = "Poupança", InitialBalance = 200m,
            Type = AccountType.Savings
        });
        db.Budgets.AddRange(
            new Budget { UserId = userId, CategoryId = food.Id, Year = 2026, Month = 11, LimitAmount = 200m },
            new Budget { UserId = userId, CategoryId = travel.Id, Year = 2026, Month = 11, LimitAmount = 100m },
            new Budget { UserId = userId, CategoryId = food.Id, Year = 2026, Month = 12, LimitAmount = 1000m });
        db.Transactions.AddRange(
            Transaction(userId, account, salary, 100m, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)),
            Transaction(userId, account, food, 20m, new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc)),
            Transaction(userId, account, food, 45.50m, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)),
            Transaction(userId, account, travel, 65m, new DateTime(2026, 11, 30, 23, 59, 59, 999, DateTimeKind.Utc)),
            Transaction(userId, account, leisure, 75m, new DateTime(2026, 11, 20, 15, 0, 0, DateTimeKind.Utc)),
            Transaction(userId, account, food, 40m, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)));

        var otherUserId = Guid.NewGuid();
        var (otherAccount, otherFood, _, _, otherSalary) = SeedUser(db, otherUserId, 900000m);
        db.Budgets.Add(new Budget
        {
            UserId = otherUserId, CategoryId = otherFood.Id,
            Year = 2026, Month = 11, LimitAmount = 1m
        });
        db.Transactions.AddRange(
            Transaction(otherUserId, otherAccount, otherFood, 10000m,
                new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)),
            Transaction(otherUserId, otherAccount, otherSalary, 50000m,
                new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var response = GetResponse(await NewController(db, userId)
            .GetMonthly(2026, 11, CancellationToken.None));

        Assert.Equal(2026, response.Year);
        Assert.Equal(11, response.Month);
        Assert.Equal(1054.50m, response.TotalBalance); // 1200 + 100 - (20 + 45.50 + 65 + 75 + 40)
        Assert.Equal(185.50m, response.MonthlyExpenses); // inclui os 75 sem orçamento
        Assert.Equal(300m, response.TotalBudget);
        Assert.Equal(110.50m, response.BudgetedExpenses);
        Assert.Equal(36.83m, response.BudgetUsagePercentage);
    }

    [Fact]
    public async Task GetMonthly_NoData_ReturnsZeroes()
    {
        await using var db = NewDatabase();
        var response = GetResponse(await NewController(db, Guid.NewGuid())
            .GetMonthly(2026, 11, CancellationToken.None));

        Assert.Equal(0m, response.TotalBalance);
        Assert.Equal(0m, response.MonthlyExpenses);
        Assert.Equal(0m, response.TotalBudget);
        Assert.Equal(0m, response.BudgetedExpenses);
        Assert.Equal(0m, response.BudgetUsagePercentage);
    }

    [Fact]
    public async Task GetMonthly_NoBudget_StillReportsExpensesWithoutDividingByZero()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, food, _, _, _) = SeedUser(db, userId);
        db.Transactions.Add(Transaction(userId, account, food, 35m,
            new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var response = GetResponse(await NewController(db, userId)
            .GetMonthly(2026, 11, CancellationToken.None));

        Assert.Equal(965m, response.TotalBalance);
        Assert.Equal(35m, response.MonthlyExpenses);
        Assert.Equal(0m, response.TotalBudget);
        Assert.Equal(0m, response.BudgetedExpenses);
        Assert.Equal(0m, response.BudgetUsagePercentage);
    }

    [Fact]
    public async Task GetMonthly_BudgetExceeded_CanReturnMoreThanOneHundredPercent()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, food, _, _, _) = SeedUser(db, userId);
        db.Budgets.Add(new Budget
        {
            UserId = userId, CategoryId = food.Id,
            Year = 2026, Month = 11, LimitAmount = 60m
        });
        db.Transactions.Add(Transaction(userId, account, food, 85m,
            new DateTime(2026, 11, 30, 23, 59, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var response = GetResponse(await NewController(db, userId)
            .GetMonthly(2026, 11, CancellationToken.None));

        Assert.Equal(141.67m, response.BudgetUsagePercentage);
    }

    [Fact]
    public async Task GetMonthly_DecemberOfYear9999_DoesNotOverflow()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, food, _, _, _) = SeedUser(db, userId);
        db.Transactions.Add(Transaction(userId, account, food, 20m,
            new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var response = GetResponse(await NewController(db, userId)
            .GetMonthly(9999, 12, CancellationToken.None));

        Assert.Equal(20m, response.MonthlyExpenses);
    }

    [Fact]
    public async Task GetMonthly_WithoutParameters_UsesCurrentUtcMonth()
    {
        await using var db = NewDatabase();
        var before = DateTime.UtcNow;
        var response = GetResponse(await NewController(db, Guid.NewGuid())
            .GetMonthly(null, null, CancellationToken.None));
        var after = DateTime.UtcNow;

        // Tolera uma eventual mudança de mês entre as duas leituras do relógio.
        var reported = new DateTime(response.Year, response.Month, 1);
        var monthBefore = new DateTime(before.Year, before.Month, 1);
        var monthAfter = new DateTime(after.Year, after.Month, 1);
        Assert.InRange(reported, monthBefore, monthAfter);
    }

    [Theory]
    [InlineData(2026, null)]
    [InlineData(null, 11)]
    [InlineData(0, 11)]
    [InlineData(10000, 11)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task GetMonthly_InvalidOrIncompletePeriod_Returns400(int? year, int? month)
    {
        await using var db = NewDatabase();
        var result = await NewController(db, Guid.NewGuid())
            .GetMonthly(year, month, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMonthly_WithoutUserClaim_Returns401()
    {
        await using var db = NewDatabase();
        var result = await NewController(db, null)
            .GetMonthly(2026, 11, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}

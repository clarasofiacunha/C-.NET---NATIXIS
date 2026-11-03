using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Controllers;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Accounts;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Tests;

// Testes ao comportamento do AccountsController, sem tocar no PostgreSQL real.
public sealed class AccountsControllerTests
{
    private static AppDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"accounts-test-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static AccountsController NewController(AppDbContext db, Guid? userId)
    {
        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims, userId.HasValue ? "TestAuth" : null);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return new AccountsController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static AppUser NewUser(Guid id) => new()
    {
        Id = id,
        UserName = $"user-{id:N}@example.test",
        Email = $"user-{id:N}@example.test",
        Name = "Utilizador de teste"
    };

    private static Account NewAccount(Guid userId, string name = "Conta à ordem") => new()
    {
        UserId = userId,
        Name = name,
        Type = AccountType.Checking,
        InitialBalance = 100m
    };

    [Fact]
    public async Task Create_ValidRequest_Returns201AndPersistsAccount()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Users.Add(NewUser(userId));
        await db.SaveChangesAsync();
        var controller = NewController(db, userId);

        var result = await controller.Create(new AccountRequestDto
        {
            Name = "  Poupança  ",
            Type = AccountType.Savings,
            InitialBalance = 1500m
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var account = Assert.IsType<AccountResponseDto>(created.Value);
        Assert.Equal("Poupança", account.Name);
        Assert.Equal(AccountType.Savings, account.Type);
        Assert.Equal(userId, (await db.Accounts.SingleAsync()).UserId);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyAuthenticatedUsersAccounts()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Users.AddRange(NewUser(userId), NewUser(otherUserId));
        db.Accounts.AddRange(NewAccount(userId, "Minha conta"), NewAccount(otherUserId, "Conta alheia"));
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var accounts = Assert.IsType<List<AccountResponseDto>>(ok.Value);
        Assert.Single(accounts);
        Assert.Equal("Minha conta", accounts[0].Name);
    }

    [Fact]
    public async Task GetById_ExistingAccount_Returns200()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Users.Add(NewUser(userId));
        var account = NewAccount(userId);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).GetById(account.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(account.Id, Assert.IsType<AccountResponseDto>(ok.Value).Id);
    }

    [Fact]
    public async Task GetById_OtherUsersAccount_Returns404()
    {
        await using var db = NewDatabase();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        db.Users.AddRange(NewUser(ownerId), NewUser(otherId));
        var account = NewAccount(ownerId);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var result = await NewController(db, otherId).GetById(account.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400()
    {
        await using var db = NewDatabase();
        var controller = NewController(db, Guid.NewGuid());

        var result = await controller.Create(new AccountRequestDto
        {
            Name = "  ",
            Type = AccountType.Cash,
            InitialBalance = 20m
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.Accounts);
    }

    [Fact]
    public async Task Create_InitialBalanceWithThreeDecimals_Returns400()
    {
        await using var db = NewDatabase();
        var result = await NewController(db, Guid.NewGuid()).Create(new AccountRequestDto
        {
            Name = "Teste",
            Type = AccountType.Cash,
            InitialBalance = 1.234m
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ExistingAccount_Returns200AndPersistsChanges()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Users.Add(NewUser(userId));
        var account = NewAccount(userId);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Update(account.Id, new AccountRequestDto
        {
            Name = "Conta Poupança",
            Type = AccountType.Savings,
            InitialBalance = 200m
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Conta Poupança", Assert.IsType<AccountResponseDto>(ok.Value).Name);
        var saved = await db.Accounts.SingleAsync();
        Assert.Equal(AccountType.Savings, saved.Type);
        Assert.Equal(200m, saved.InitialBalance);
    }

    [Fact]
    public async Task Delete_AccountWithoutTransactions_Returns204()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Users.Add(NewUser(userId));
        var account = NewAccount(userId);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Delete(account.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Accounts);
    }

    [Fact]
    public async Task Delete_AccountWithTransactions_Returns409AndKeepsAccount()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Users.Add(NewUser(userId));
        var account = NewAccount(userId);
        var category = new Category
        {
            UserId = userId,
            Name = "Alimentação",
            Type = TransactionType.Expense
        };
        db.Accounts.Add(account);
        db.Categories.Add(category);
        db.Transactions.Add(new FinanceTransaction
        {
            UserId = userId,
            AccountId = account.Id,
            CategoryId = category.Id,
            Amount = 15m,
            Type = TransactionType.Expense
        });
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Delete(account.Id, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.True(await db.Accounts.AnyAsync(a => a.Id == account.Id));
    }

    [Fact]
    public async Task Delete_UnknownAccount_Returns404()
    {
        await using var db = NewDatabase();

        var result = await NewController(db, Guid.NewGuid())
            .Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAll_WithoutUserIdClaim_Returns401()
    {
        await using var db = NewDatabase();

        var result = await NewController(db, userId: null).GetAll(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}

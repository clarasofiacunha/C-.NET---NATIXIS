using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Controllers;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Categories;
using PersonalFinance.Api.Models;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Tests;

public sealed class CategoriesControllerTests
{
    private static AppDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"categories-test-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static CategoriesController NewController(AppDbContext db, Guid? userId)
    {
        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims, userId.HasValue ? "TestAuth" : null);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return new CategoriesController(new CategoryService(db))
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

    private static Category NewCategory(
        Guid userId, string name = "Alimentação", TransactionType type = TransactionType.Expense) => new()
    {
        UserId = userId,
        Name = name,
        Type = type
    };

    [Fact]
    public async Task GetAll_ReturnsOnlyAuthenticatedUsersCategories()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Categories.AddRange(
            NewCategory(userId, "Alimentação"),
            NewCategory(Guid.NewGuid(), "Privada"));
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).GetAll(CancellationToken.None);

        var categories = Assert.IsType<List<CategoryResponseDto>>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        var category = Assert.Single(categories);
        Assert.Equal("Alimentação", category.Name);
    }

    [Fact]
    public async Task GetById_OtherUsersCategory_Returns404()
    {
        await using var db = NewDatabase();
        var ownerId = Guid.NewGuid();
        var category = NewCategory(ownerId);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var result = await NewController(db, Guid.NewGuid())
            .GetById(category.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201AndTrimsName()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var result = await NewController(db, userId).Create(
            new CategoryRequestDto { Name = "  Saúde  ", Type = TransactionType.Expense },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var category = Assert.IsType<CategoryResponseDto>(created.Value);
        Assert.Equal("Saúde", category.Name);
        Assert.Equal(userId, (await db.Categories.SingleAsync()).UserId);
    }

    [Fact]
    public async Task Create_DuplicateNameAndType_Returns409()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Categories.Add(NewCategory(userId));
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Create(
            new CategoryRequestDto { Name = "Alimentação", Type = TransactionType.Expense },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task Update_ValidRequest_Returns200AndPersistsChanges()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var category = NewCategory(userId);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Update(
            category.Id,
            new CategoryRequestDto { Name = "Mercearia", Type = TransactionType.Expense },
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Mercearia", Assert.IsType<CategoryResponseDto>(response.Value).Name);
        Assert.Equal("Mercearia", (await db.Categories.SingleAsync()).Name);
    }

    [Fact]
    public async Task Update_UsedCategoryCannotChangeType()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        db.Users.Add(NewUser(userId));
        var account = new Account { UserId = userId, Name = "Conta", Type = AccountType.Cash };
        var category = NewCategory(userId);
        db.Accounts.Add(account);
        db.Categories.Add(category);
        db.Transactions.Add(new FinanceTransaction
        {
            UserId = userId,
            AccountId = account.Id,
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Amount = 10m
        });
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Update(
            category.Id,
            new CategoryRequestDto { Name = "Rendimento", Type = TransactionType.Income },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(TransactionType.Expense, (await db.Categories.SingleAsync()).Type);
    }

    [Fact]
    public async Task Delete_CategoryWithBudget_Returns409AndKeepsCategory()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var category = NewCategory(userId);
        db.Categories.Add(category);
        db.Budgets.Add(new Budget
        {
            UserId = userId,
            CategoryId = category.Id,
            Month = 1,
            Year = 2026,
            LimitAmount = 100m
        });
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Delete(category.Id, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task Delete_UnusedCategory_Returns204()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var category = NewCategory(userId);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Delete(category.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Categories);
    }
}

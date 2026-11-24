using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Controllers;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs;
using PersonalFinance.Api.DTOs.Transactions;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Tests;

// Testes unitarios do CRUD (sem PostgreSQL). A autenticacao e simulada via ClaimsPrincipal.
public sealed class TransactionsControllerTests
{
    private static AppDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"transactions-test-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static TransactionsController NewController(AppDbContext db, Guid? userId)
    {
        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : Array.Empty<Claim>();
        return new TransactionsController(db)
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

    private static (Account Account, Category Expense, Category Income) Seed(
        AppDbContext db, Guid userId)
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
            UserId = userId, Name = "Conta à ordem", Type = AccountType.Checking,
            InitialBalance = 100m, Balance = 100m
        };
        var expense = new Category { UserId = userId, Name = "Alimentação", Type = TransactionType.Expense };
        var income = new Category { UserId = userId, Name = "Salário", Type = TransactionType.Income };
        db.Accounts.Add(account);
        db.Categories.AddRange(expense, income);
        db.SaveChanges();
        return (account, expense, income);
    }

    private static TransactionRequestDto Request(Account account, Category category,
        decimal amount = 20.50m, TransactionType type = TransactionType.Expense) => new()
    {
        AccountId = account.Id,
        CategoryId = category.Id,
        Amount = amount,
        Type = type,
        Date = new DateTimeOffset(2026, 11, 3, 12, 30, 0, TimeSpan.FromHours(1)),
        Description = "  Compras  "
    };

    private static FinanceTransaction Transaction(Guid userId, Account account,
        Category category, decimal amount, DateTime date) => new()
    {
        UserId = userId,
        AccountId = account.Id,
        CategoryId = category.Id,
        Amount = amount,
        Date = date,
        Type = category.Type,
        Description = "Teste"
    };

    [Fact]
    public async Task Create_ValidExpense_Returns201AndPersistsUtcDate()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);

        var result = await NewController(db, userId)
            .Create(Request(account, expense), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var response = Assert.IsType<TransactionResponseDto>(created.Value);
        Assert.Equal(account.Id, response.AccountId);
        Assert.Equal("Conta à ordem", response.AccountName);
        Assert.Equal("Alimentação", response.CategoryName);
        Assert.Equal("Compras", response.Description);
        Assert.Equal(new DateTime(2026, 11, 3, 11, 30, 0, DateTimeKind.Utc), response.Date);
        var saved = await db.Transactions.SingleAsync();
        Assert.Equal(userId, saved.UserId);
        Assert.Equal(DateTimeKind.Utc, saved.Date.Kind);
        Assert.Equal(79.50m, (await db.Accounts.SingleAsync()).Balance);
    }

    [Fact]
    public async Task Create_IncomeTransaction_IncreasesAccountBalance()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, _, income) = Seed(db, userId);

        await NewController(db, userId).Create(
            Request(account, income, 250m, TransactionType.Income), CancellationToken.None);

        Assert.Equal(350m, (await db.Accounts.SingleAsync()).Balance);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyOwnTransactionsAndFiltersInclusiveDatesAndType()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, income) = Seed(db, userId);
        var otherId = Guid.NewGuid();
        var (otherAccount, otherExpense, _) = Seed(db, otherId);

        db.Transactions.AddRange(
            Transaction(userId, account, expense, 10m, new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)),
            Transaction(userId, account, expense, 20m, new DateTime(2026, 11, 30, 23, 59, 0, DateTimeKind.Utc)),
            Transaction(userId, account, expense, 30m, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)),
            Transaction(userId, account, income, 100m, new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc)),
            Transaction(otherId, otherAccount, otherExpense, 50m, new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).GetAll(new TransactionQueryParameters
        {
            From = new DateOnly(2026, 11, 1), To = new DateOnly(2026, 11, 30),
            Type = TransactionType.Expense, AccountId = account.Id, CategoryId = expense.Id
        }, CancellationToken.None);

        var items = Items(result);
        Assert.Equal(2, items.Count);
        Assert.Equal(new[] { 20m, 10m }, items.Select(t => t.Amount));
    }

    [Fact]
    public async Task GetAll_WithoutFilters_DoesNotExposeOtherUsersTransactions()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var otherId = Guid.NewGuid();
        var (otherAccount, otherExpense, _) = Seed(db, otherId);
        db.Transactions.AddRange(
            Transaction(userId, account, expense, 10m, DateTime.UtcNow),
            Transaction(otherId, otherAccount, otherExpense, 99m, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var result = await NewController(db, userId)
            .GetAll(new TransactionQueryParameters(), CancellationToken.None);

        var items = Items(result);
        Assert.Single(items);
        Assert.Equal(10m, items[0].Amount);
    }

    [Fact]
    public async Task GetAll_FromAfterTo_Returns400()
    {
        await using var db = NewDatabase();
        var result = await NewController(db, Guid.NewGuid()).GetAll(new TransactionQueryParameters
        {
            From = new DateOnly(2026, 12, 1), To = new DateOnly(2026, 11, 1)
        }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_AnotherUsersTransaction_Returns404()
    {
        await using var db = NewDatabase();
        var ownerId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, ownerId);
        var otherId = Guid.NewGuid();
        Seed(db, otherId);
        var transaction = Transaction(ownerId, account, expense, 10m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await NewController(db, otherId)
            .GetById(transaction.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_OwnTransaction_Returns200()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var transaction = Transaction(userId, account, expense, 10m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId)
            .GetById(transaction.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(transaction.Id, Assert.IsType<TransactionResponseDto>(ok.Value).Id);
    }

    [Fact]
    public async Task Update_OwnTransaction_CanChangeToIncomeAndPersists()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, income) = Seed(db, userId);
        account.Balance = 90m;
        var transaction = Transaction(userId, account, expense, 10m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Update(transaction.Id,
            Request(account, income, 300m, TransactionType.Income), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(TransactionType.Income, Assert.IsType<TransactionResponseDto>(ok.Value).Type);
        var saved = await db.Transactions.SingleAsync();
        Assert.Equal(300m, saved.Amount);
        Assert.Equal(income.Id, saved.CategoryId);
        Assert.Equal(TransactionType.Income, saved.Type);
        Assert.Equal(400m, (await db.Accounts.SingleAsync()).Balance);
    }

    [Fact]
    public async Task Update_ExpenseAmount_RecalculatesAccountBalance()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        account.Balance = 950m;
        var transaction = Transaction(userId, account, expense, 50m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        await NewController(db, userId).Update(transaction.Id,
            Request(account, expense, 80m), CancellationToken.None);

        Assert.Equal(920m, (await db.Accounts.SingleAsync()).Balance);
    }

    [Fact]
    public async Task Update_MoveToAnotherAccount_AdjustsBothBalances()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (sourceAccount, expense, _) = Seed(db, userId);
        sourceAccount.Balance = 950m;
        var destinationAccount = new Account
        {
            UserId = userId, Name = "Poupança", Type = AccountType.Savings,
            InitialBalance = 300m, Balance = 300m
        };
        db.Accounts.Add(destinationAccount);
        var transaction = Transaction(userId, sourceAccount, expense, 50m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        await NewController(db, userId).Update(transaction.Id,
            Request(destinationAccount, expense, 80m, TransactionType.Income), CancellationToken.None);

        Assert.Equal(1000m, (await db.Accounts.SingleAsync(a => a.Id == sourceAccount.Id)).Balance);
        Assert.Equal(380m, (await db.Accounts.SingleAsync(a => a.Id == destinationAccount.Id)).Balance);
    }

    [Fact]
    public async Task Create_IncomeTransactionInExpenseCategory_IsAllowed()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);

        var result = await NewController(db, userId).Create(
            Request(account, expense, 20m, TransactionType.Income), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<TransactionResponseDto>(created.Value);
        Assert.Equal(expense.Id, response.CategoryId);
        Assert.Equal(TransactionType.Income, response.Type);
        var transaction = await db.Transactions.SingleAsync();
        Assert.Equal(expense.Id, transaction.CategoryId);
        Assert.Equal(TransactionType.Income, transaction.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.123)]
    public async Task Create_InvalidAmount_Returns400(double amount)
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);

        var result = await NewController(db, userId).Create(
            Request(account, expense, (decimal)amount), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task Create_MissingDateOrEmptyAccount_Returns400()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var request = Request(account, expense);
        request.Date = null;
        Assert.IsType<BadRequestObjectResult>((await NewController(db, userId)
            .Create(request, CancellationToken.None)).Result);

        request = Request(account, expense);
        request.AccountId = Guid.Empty;
        Assert.IsType<BadRequestObjectResult>((await NewController(db, userId)
            .Create(request, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Create_OtherUsersAccountOrCategory_Returns404()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var otherId = Guid.NewGuid();
        var (otherAccount, otherExpense, _) = Seed(db, otherId);

        var resultAccount = await NewController(db, userId).Create(
            Request(otherAccount, expense), CancellationToken.None);
        var resultCategory = await NewController(db, userId).Create(
            Request(account, otherExpense), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(resultAccount.Result);
        Assert.IsType<NotFoundObjectResult>(resultCategory.Result);
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task Update_AnotherUsersTransaction_Returns404()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var otherId = Guid.NewGuid();
        var (otherAccount, otherExpense, _) = Seed(db, otherId);
        var transaction = Transaction(otherId, otherAccount, otherExpense, 15m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Update(transaction.Id,
            Request(account, expense), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(15m, (await db.Transactions.SingleAsync()).Amount);
    }

    [Fact]
    public async Task Update_ExpenseTransactionInIncomeCategory_IsAllowed()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, income) = Seed(db, userId);
        var transaction = Transaction(userId, account, expense, 10m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId).Update(transaction.Id,
            Request(account, income, 100m, TransactionType.Expense), CancellationToken.None);

        var updated = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TransactionResponseDto>(updated.Value);
        Assert.Equal(income.Id, response.CategoryId);
        Assert.Equal(TransactionType.Expense, response.Type);
        var saved = await db.Transactions.SingleAsync();
        Assert.Equal(100m, saved.Amount);
        Assert.Equal(income.Id, saved.CategoryId);
        Assert.Equal(TransactionType.Expense, saved.Type);
    }

    [Fact]
    public async Task Create_DescriptionTooLong_Returns400()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var request = Request(account, expense);
        request.Description = new string('x', 501);

        var result = await NewController(db, userId).Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task Delete_OwnTransaction_Returns204AndRemovesIt()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        account.Balance = 90m;
        var transaction = Transaction(userId, account, expense, 10m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await NewController(db, userId)
            .Delete(transaction.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Transactions);
        Assert.Equal(100m, (await db.Accounts.SingleAsync()).Balance);
    }

    [Fact]
    public async Task Delete_AnotherUsersTransaction_Returns404AndKeepsIt()
    {
        await using var db = NewDatabase();
        var ownerId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, ownerId);
        var transaction = Transaction(ownerId, account, expense, 10m, DateTime.UtcNow);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await NewController(db, Guid.NewGuid())
            .Delete(transaction.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(db.Transactions);
    }

    [Fact]
    public async Task GetAll_WithoutUserClaim_Returns401()
    {
        await using var db = NewDatabase();
        var result = await NewController(db, null)
            .GetAll(new TransactionQueryParameters(), CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    private static IReadOnlyList<TransactionResponseDto> Items(
        ActionResult<PagedResponse<TransactionResponseDto>> result) => Page(result).Items;

    private static PagedResponse<TransactionResponseDto> Page(
        ActionResult<PagedResponse<TransactionResponseDto>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<PagedResponse<TransactionResponseDto>>(ok.Value);
    }

    private static async Task<(AppDbContext Db, Guid UserId)> SeedManyAsync(int count)
    {
        var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var start = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        for (var i = 1; i <= count; i++)
            db.Transactions.Add(Transaction(userId, account, expense, i, start.AddDays(i)));
        await db.SaveChangesAsync();
        return (db, userId);
    }

    [Fact]
    public async Task GetAll_PaginatesWithDefaultPageSizeAndMetadata()
    {
        var (db, userId) = await SeedManyAsync(45);
        await using var _ = db;

        var first = Page(await NewController(db, userId)
            .GetAll(new TransactionQueryParameters(), CancellationToken.None));
        Assert.Equal(20, first.Items.Count);
        Assert.Equal(1, first.Page);
        Assert.Equal(45, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(45m, first.Items[0].Amount); // mais recente primeiro

        var last = Page(await NewController(db, userId)
            .GetAll(new TransactionQueryParameters { Page = 3 }, CancellationToken.None));
        Assert.Equal(new[] { 5m, 4m, 3m, 2m, 1m }, last.Items.Select(t => t.Amount));

        var beyond = Page(await NewController(db, userId)
            .GetAll(new TransactionQueryParameters { Page = 10 }, CancellationToken.None));
        Assert.Empty(beyond.Items);
        Assert.Equal(45, beyond.TotalCount);
    }

    [Fact]
    public async Task GetAll_FiltersByMinAndMaxAmount()
    {
        var (db, userId) = await SeedManyAsync(10);
        await using var _ = db;

        var items = Items(await NewController(db, userId).GetAll(
            new TransactionQueryParameters { MinAmount = 3m, MaxAmount = 6m }, CancellationToken.None));

        Assert.Equal(new[] { 6m, 5m, 4m, 3m }, items.Select(t => t.Amount));
    }

    [Fact]
    public async Task GetAll_FiltersByAccountType()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (checking, expense, _) = Seed(db, userId);
        var savings = new Account
        {
            UserId = userId, Name = "Savings", Type = AccountType.Savings,
            InitialBalance = 0m, Balance = 0m
        };
        db.Accounts.Add(savings);
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Transactions.AddRange(
            Transaction(userId, checking, expense, 10m, day),
            Transaction(userId, savings, expense, 20m, day));
        await db.SaveChangesAsync();

        var items = Items(await NewController(db, userId).GetAll(
            new TransactionQueryParameters { AccountType = AccountType.Savings }, CancellationToken.None));

        Assert.Equal(20m, Assert.Single(items).Amount);
    }

    [Fact]
    public async Task GetAll_SortsByAmountAndDirection()
    {
        await using var db = NewDatabase();
        var userId = Guid.NewGuid();
        var (account, expense, _) = Seed(db, userId);
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Transactions.AddRange(
            Transaction(userId, account, expense, 50m, day),
            Transaction(userId, account, expense, 5m, day.AddDays(2)),
            Transaction(userId, account, expense, 500m, day.AddDays(1)));
        await db.SaveChangesAsync();

        var mostExpensive = Items(await NewController(db, userId).GetAll(
            new TransactionQueryParameters { SortBy = TransactionSortField.Amount }, CancellationToken.None));
        Assert.Equal(new[] { 500m, 50m, 5m }, mostExpensive.Select(t => t.Amount));

        var oldest = Items(await NewController(db, userId).GetAll(
            new TransactionQueryParameters { SortDirection = SortDirection.Asc }, CancellationToken.None));
        Assert.Equal(new[] { 50m, 500m, 5m }, oldest.Select(t => t.Amount));
    }

    [Theory]
    [InlineData(0, 20, null, null)]
    [InlineData(1, 0, null, null)]
    [InlineData(1, 101, null, null)]
    [InlineData(1, 20, 10.0, 5.0)]
    [InlineData(1, 20, -1.0, null)]
    public async Task GetAll_InvalidPagingOrAmountRange_Returns400(
        int page, int pageSize, double? min, double? max)
    {
        await using var db = NewDatabase();
        var result = await NewController(db, Guid.NewGuid()).GetAll(new TransactionQueryParameters
        {
            Page = page, PageSize = pageSize,
            MinAmount = (decimal?)min, MaxAmount = (decimal?)max
        }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}

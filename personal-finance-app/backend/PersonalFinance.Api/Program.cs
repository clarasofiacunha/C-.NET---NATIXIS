using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers existentes no backend continuam acessíveis.
builder.Services.AddControllers();

// Permitir pedidos vindos do frontend local (Vite e React, por exemplo).
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy => policy
        .WithOrigins(
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// A connection string deve estar em User Secrets (desenvolvimento) ou em variáveis
// de ambiente. Não guardar passwords da base de dados no repositório.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Falta a connection string 'DefaultConnection'. " +
        "Configura-a com: dotnet user-secrets set " +
        "\"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=personal_finance;Username=personal_dev;Password=...\"");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity gere utilizadores e hashes de passwords. Sem bibliotecas de IA.
builder.Services
    .AddIdentityCore<AppUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Autenticação por cookie apenas como configuração base para Controllers
// que usem [Authorize]. Se o projeto já usar JWT, preservar a configuração JWT.
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Seed APENAS por comando explícito, em ambiente Development:
// dotnet run -- --seed
if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "O --seed está disponível apenas em ambiente Development.");
    }

    using var scope = app.Services.CreateScope();
    await DemoSeeder.SeedAsync(scope.ServiceProvider);
    app.Logger.LogInformation("Dados de demonstração verificados/inseridos.");
}

app.UseCors("LocalFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Teste rápido: não depende da base de dados.
app.MapGet("/", () => Results.Ok(new
{
    application = "Personal Finance API",
    status = "online",
    utc = DateTime.UtcNow
}));

// Teste da ligação ao PostgreSQL. Não divulga credenciais ou detalhes internos.
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        return await db.Database.CanConnectAsync()
            ? Results.Ok(new { api = "online", database = "connected" })
            : Results.Json(
                new { api = "online", database = "unavailable" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(
            new { api = "online", database = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// Endpoints de diagnóstico: nunca expor dados pessoais em produção.
if (app.Environment.IsDevelopment())
{
    var dev = app.MapGroup("/dev");

    // Contagens por entidade. Confirma que as tabelas e o seed existem.
    dev.MapGet("/db", async (AppDbContext db) => Results.Ok(new
    {
        users = await db.Users.CountAsync(),
        accounts = await db.Accounts.CountAsync(),
        categories = await db.Categories.CountAsync(),
        transactions = await db.Transactions.CountAsync(),
        budgets = await db.Budgets.CountAsync(),
        goals = await db.Goals.CountAsync()
    }));

    // Não devolve hashes de passwords ou outros campos sensíveis do Identity.
    dev.MapGet("/users", async (AppDbContext db) =>
        Results.Ok(await db.Users.AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, u.Email, u.CreatedAt })
            .ToListAsync()));

    // Testa a verificação do hash, sem criar sessões/tokens.
    // Isto NÃO substitui um endpoint de login real.
    dev.MapPost("/auth/check", async (
        LoginCheckRequest request,
        UserManager<AppUser> userManager) =>
    {
        var user = await userManager.FindByEmailAsync(request.Email ?? "");
        var valid = user is not null
            && await userManager.CheckPasswordAsync(user, request.Password ?? "");

        return Results.Ok(new { passwordCorrect = valid });
    });

    dev.MapGet("/users/{userId:guid}/accounts", async (
        Guid userId, AppDbContext db) =>
        Results.Ok(await db.Accounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id, a.Name, a.Type, a.InitialBalance, a.CreatedAt
            })
            .ToListAsync()));

    dev.MapGet("/users/{userId:guid}/categories", async (
        Guid userId, AppDbContext db) =>
        Results.Ok(await db.Categories.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Type })
            .ToListAsync()));

    dev.MapGet("/users/{userId:guid}/transactions", async (
        Guid userId, AppDbContext db) =>
        Results.Ok(await db.Transactions.AsNoTracking()
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
            .ToListAsync()));

    dev.MapGet("/users/{userId:guid}/budgets", async (
        Guid userId, AppDbContext db) =>
        Results.Ok(await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .Select(b => new
            {
                b.Id, b.Year, b.Month, b.LimitAmount,
                b.CategoryId,
                category = b.Category.Name
            })
            .ToListAsync()));

    dev.MapGet("/users/{userId:guid}/goals", async (
        Guid userId, AppDbContext db) =>
        Results.Ok(await db.Goals.AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Deadline)
            .Select(g => new
            {
                g.Id, g.Name, g.TargetAmount, g.CurrentAmount, g.Deadline
            })
            .ToListAsync()));

    // Valores do dashboard para o mês indicado. Exemplo:
    // /dev/users/{id}/dashboard?year=2026&month=10
    dev.MapGet("/users/{userId:guid}/dashboard", async (
        Guid userId, int? year, int? month, AppDbContext db) =>
    {
        var today = DateTime.UtcNow;
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (y < 1 || y > 9998 || m < 1 || m > 12)
            return Results.BadRequest(new { error = "Ano ou mês inválido." });

        var exists = await db.Users.AnyAsync(u => u.Id == userId);
        if (!exists)
            return Results.NotFound(new { error = "Utilizador não encontrado." });

        var start = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
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
            .Where(b => b.UserId == userId && b.Year == y && b.Month == m)
            .SumAsync(b => (decimal?)b.LimitAmount) ?? 0m;

        return Results.Ok(new
        {
            year = y, month = m,
            income,
            expense,
            balanceForMonth = income - expense,
            totalBudgetLimit = budgetLimit
        });
    });
}

// Inclui os Controllers já existentes na tua API.
app.MapControllers();

app.Run();

public sealed record LoginCheckRequest(string? Email, string? Password);

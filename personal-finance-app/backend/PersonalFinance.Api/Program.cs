using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy => policy
        .WithOrigins(
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Falta a connection string 'DefaultConnection'. " +
        "Configura-a com: dotnet user-secrets set " +
        "\"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=personal_finance;Username=personal_dev;Password=...\"");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddIdentityCore<AppUser>()
    .AddSignInManager()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "pf.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
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

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("O --seed esta disponivel apenas em ambiente Development.");

    using var scope = app.Services.CreateScope();
    await DemoSeeder.SeedAsync(scope.ServiceProvider);
    app.Logger.LogInformation("Dados de demonstracao verificados/inseridos.");
}

app.UseCors("LocalFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

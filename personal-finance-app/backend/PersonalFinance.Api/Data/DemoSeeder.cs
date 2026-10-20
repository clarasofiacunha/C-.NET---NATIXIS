using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Data;

public static class DemoSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var rng = new Random(20261020);
        var faker = new Faker("pt_PT");
        var profiles = new (string Name, decimal Salary, bool Variable)[]
        {
            ("Ana Estudante", 650m, false),
            ("Bruno Profissional", 1450m, false),
            ("Carla Sénior", 2600m, false),
            ("Diogo Freelancer", 1700m, true)
        };
        var expenseNames = new[] { "Habitação", "Alimentação", "Transportes", "Lazer", "Saúde", "Subscrições", "Outros" };
        var expenseLimits = new[] { 600m, 250m, 80m, 120m, 75m, 40m, 90m };
        var expenseDescriptions = new Dictionary<string, string[]>
        {
            ["Habitação"] = new[] { "Renda", "Eletricidade", "Água" },
            ["Alimentação"] = new[] { "Continente", "Pingo Doce", "Mercado local" },
            ["Transportes"] = new[] { "Andante", "Combustível", "Bilhete de comboio" },
            ["Lazer"] = new[] { "Cinema", "Restaurante", "Café" },
            ["Saúde"] = new[] { "Farmácia", "Consulta" },
            ["Subscrições"] = new[] { "Streaming", "Cloud" },
            ["Outros"] = new[] { "Compras diversas", "Presente" }
        };
        for (int i = 0; i < profiles.Length; i++)
        {
            var email = $"demo{i + 1}@example.test";
            var user = await users.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser { Id = Guid.NewGuid(), Name = profiles[i].Name, Email = email, UserName = email, EmailConfirmed = true };
                var created = await users.CreateAsync(user, "Demo123!Change");
                if (!created.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
            }
            if (await db.Accounts.AnyAsync(a => a.UserId == user.Id)) continue;

            var checking = new Account { UserId = user.Id, Name = "Conta à ordem", Type = AccountType.Checking, InitialBalance = 2000m };
            var savings = new Account { UserId = user.Id, Name = "Poupança", Type = AccountType.Savings, InitialBalance = 1000m };
            var categories = expenseNames.Select(n => new Category { UserId = user.Id, Name = n, Type = TransactionType.Expense }).ToList();
            var income = new Category { UserId = user.Id, Name = profiles[i].Variable ? "Trabalho independente" : "Salário", Type = TransactionType.Income };
            db.Accounts.AddRange(checking, savings);
            db.Categories.AddRange(categories);
            db.Categories.Add(income);
            var start = new DateTime(2025, 11, 1, 12, 0, 0, DateTimeKind.Utc);
            for (int month = 0; month < 12; month++)
            {
                var date = start.AddMonths(month);
                var salary = profiles[i].Variable ? rng.Next(900, 3201) : profiles[i].Salary;
                db.Transactions.Add(new FinanceTransaction { UserId = user.Id, Account = checking, Category = income, Type = TransactionType.Income, Amount = salary, Date = date, Description = profiles[i].Variable ? "Prestação de serviços" : "Salário / rendimento" });
                for (int c = 0; c < categories.Count; c++)
                {
                    var category = categories[c];
                    var budget = expenseLimits[c] * (i == 0 ? 0.65m : i == 2 ? 1.3m : 1m);
                    db.Budgets.Add(new Budget { UserId = user.Id, Category = category, Month = date.Month, Year = date.Year, LimitAmount = decimal.Round(budget, 2) });
                    var count = c == 1 ? 7 : c == 0 ? 2 : 3;
                    for (int t = 0; t < count; t++)
                    {
                        var scale = c == 0 ? 0.55m : c == 1 ? 0.18m : 0.24m;
                        var factor = (decimal)(0.55 + rng.NextDouble() * 0.9);
                        var amount = decimal.Round(Math.Max(1m, budget * scale * factor), 2);
                        var day = rng.Next(1, DateTime.DaysInMonth(date.Year, date.Month) + 1);
                        var descriptions = expenseDescriptions[category.Name];
                        db.Transactions.Add(new FinanceTransaction { UserId = user.Id, Account = checking, Category = category, Type = TransactionType.Expense, Amount = amount, Date = new DateTime(date.Year, date.Month, day, 12, 0, 0, DateTimeKind.Utc), Description = descriptions[rng.Next(descriptions.Length)] });
                    }
                }
            }
            db.Goals.AddRange(
                new Goal { UserId = user.Id, Name = "Fundo de emergência", TargetAmount = 3000m, CurrentAmount = 950m, Deadline = new DateTime(2027, 12, 31, 12, 0, 0, DateTimeKind.Utc) },
                new Goal { UserId = user.Id, Name = "Viagem", TargetAmount = 1200m, CurrentAmount = 430m, Deadline = new DateTime(2027, 7, 1, 12, 0, 0, DateTimeKind.Utc) }
            );
            await db.SaveChangesAsync();
        }
    }
}

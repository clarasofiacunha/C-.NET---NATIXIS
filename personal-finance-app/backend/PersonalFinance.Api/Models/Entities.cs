using Microsoft.AspNetCore.Identity;

namespace PersonalFinance.Api.Models;

public class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}

public enum AccountType { Checking, Savings, Cash }
public enum TransactionType { Income, Expense }

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();
}

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}

public class FinanceTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
}

public class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal LimitAmount { get; set; }
}

public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? Deadline { get; set; }
}

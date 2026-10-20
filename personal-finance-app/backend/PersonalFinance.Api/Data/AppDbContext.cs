using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinanceTransaction> Transactions => Set<FinanceTransaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Goal> Goals => Set<Goal>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.NormalizedEmail).IsUnique();
        });
        builder.Entity<Account>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.InitialBalance).HasPrecision(18, 2);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.User).WithMany(x => x.Accounts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.Id }).IsUnique();
        });
        builder.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => new { x.UserId, x.Name, x.Type }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Id }).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.Categories).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<FinanceTransaction>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Description).HasMaxLength(500);
            e.ToTable(t => t.HasCheckConstraint("CK_Transactions_Amount", "\"Amount\" > 0"));
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasOne(x => x.User).WithMany(x => x.Transactions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Account).WithMany(x => x.Transactions).HasForeignKey(x => new { x.UserId, x.AccountId }).HasPrincipalKey(x => new { x.UserId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.Transactions).HasForeignKey(x => new { x.UserId, x.CategoryId }).HasPrincipalKey(x => new { x.UserId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Budget>(e =>
        {
            e.Property(x => x.LimitAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.UserId, x.CategoryId, x.Year, x.Month }).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Budgets_Month", "\"Month\" BETWEEN 1 AND 12");
                t.HasCheckConstraint("CK_Budgets_Limit", "\"LimitAmount\" > 0");
            });
            e.HasOne(x => x.User).WithMany(x => x.Budgets).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.Budgets).HasForeignKey(x => new { x.UserId, x.CategoryId }).HasPrincipalKey(x => new { x.UserId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Goal>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.TargetAmount).HasPrecision(18, 2);
            e.Property(x => x.CurrentAmount).HasPrecision(18, 2);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Goals_Target", "\"TargetAmount\" > 0");
                t.HasCheckConstraint("CK_Goals_Current", "\"CurrentAmount\" >= 0");
            });
            e.HasOne(x => x.User).WithMany(x => x.Goals).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

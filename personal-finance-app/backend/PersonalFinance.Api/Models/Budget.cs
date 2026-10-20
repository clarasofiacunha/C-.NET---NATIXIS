namespace PersonalFinance.Api.Models;

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

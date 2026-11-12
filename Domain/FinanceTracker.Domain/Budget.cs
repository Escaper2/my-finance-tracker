namespace FinanceTracker.Domain;
public sealed class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public DateOnly Month { get; set; }
    public decimal Limit { get; set; }
}

namespace FinanceTracker.Domain;
public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public int Threshold { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}

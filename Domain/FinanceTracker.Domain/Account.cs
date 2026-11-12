namespace FinanceTracker.Domain;
public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public decimal InitialBalance { get; set; }
    public bool IsArchived { get; set; }
}

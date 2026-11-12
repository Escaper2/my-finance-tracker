namespace FinanceTracker.Domain;
public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public TransactionType Type { get; set; }
    public string Color { get; set; } = "#52796f";
    public string Icon { get; set; } = "●";
    public bool IsArchived { get; set; }
}

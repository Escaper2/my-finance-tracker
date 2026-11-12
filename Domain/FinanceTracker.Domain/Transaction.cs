namespace FinanceTracker.Domain;
public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = "";
    public Guid? ImportJobId { get; set; }
    public int? ImportRow { get; set; }
    public string? SourceKey { get; set; }
}

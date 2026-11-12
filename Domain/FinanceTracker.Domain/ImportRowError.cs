namespace FinanceTracker.Domain;
public sealed class ImportRowError
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ImportJobId { get; set; }
    public int RowNumber { get; set; }
    public string Message { get; set; } = "";
}

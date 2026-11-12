namespace FinanceTracker.Domain;
public sealed class ImportJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string FileName { get; set; } = "";
    public string FileHash { get; set; } = "";
    public string Content { get; set; } = "";
    public ImportStatus Status { get; set; } = ImportStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int DuplicateRows { get; set; }
    public int ErrorRows { get; set; }
    public string? Failure { get; set; }
}

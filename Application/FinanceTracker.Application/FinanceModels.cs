using FinanceTracker.Domain;
namespace FinanceTracker.Application;
public sealed record AccountView(Guid Id, string Name, decimal Balance, bool IsArchived, decimal InitialBalance);
public sealed record TransactionView(Guid Id, DateOnly Date, string Account, string Category, string Icon, string Color,
    decimal Amount, TransactionType Type, string Description);
public sealed record TransactionPage(List<TransactionView> Items, int Total);
public sealed class TransactionFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public TransactionType? Type { get; set; }
    public bool Uncategorized { get; set; }
    public string Search { get; set; } = "";
    public string Sort { get; set; } = "date-desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
public sealed record BudgetView(Guid Id, Guid CategoryId, string Category, string Color, DateOnly Month, decimal Limit, decimal Spent)
{
    public decimal Remaining => Limit - Spent;
    public decimal Percent => Limit == 0 ? 0 : Spent / Limit * 100;
}
public sealed record ChartPoint(string Label, decimal Value);
public sealed record MonthPoint(string Label, decimal Income, decimal Expense);
public sealed record Report(decimal Balance, decimal Income, decimal Expense, List<ChartPoint> Categories, List<MonthPoint> Months);
public sealed record CsvRow(int Number, DateOnly Date, decimal Amount, TransactionType Type, string Description, string Category, string? BankId);
public sealed record CsvError(int Number, string Message);
public sealed record CsvPreview(List<CsvRow> Rows, List<CsvError> Errors)
{
    public int Total => Rows.Count + Errors.Count;
}


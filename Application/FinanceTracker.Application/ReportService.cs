using FinanceTracker.Domain;
namespace FinanceTracker.Application;
public sealed class ReportService(IFinanceDbFactory factory)
{
    public async Task<Report> Get(DateOnly from, DateOnly to, Guid? accountId)
    {
        FinanceRules.Require(from <= to && to.DayNumber - from.DayNumber <= 3660, "Выберите период не более 10 лет.");
        await using var db = await factory.OpenAsync();
        var all = db.Query<Transaction>();
        var accounts = db.Query<Account>();
        if (accountId.HasValue) { all = all.Where(t => t.AccountId == accountId); accounts = accounts.Where(a => a.Id == accountId); }
        var balance = (accounts.Sum(a => (decimal?)a.InitialBalance) ?? 0)
            + (all.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0)
            - (all.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0);
        var period = all.Where(t => t.Date >= from && t.Date <= to);
        var income = period.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0;
        var expense = period.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0;
        var names = db.Query<Category>().ToDictionary(c => c.Id, c => c.Name);
        var categories = period.Where(t => t.Type == TransactionType.Expense).GroupBy(t => t.CategoryId)
            .Select(g => new { Id = g.Key, Total = g.Sum(t => t.Amount) }).ToList()
            .Select(g => new ChartPoint(g.Id.HasValue ? names[g.Id.Value] : "Без категории", g.Total))
            .OrderByDescending(c => c.Value).ToList();
        var months = new List<MonthPoint>();
        for (var month = FinanceRules.MonthOf(from); month <= to; month = month.AddMonths(1))
        {
            var end = month.AddMonths(1);
            var q = period.Where(t => t.Date >= month && t.Date < end);
            months.Add(new(month.ToString("MM.yy"),
                q.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0,
                q.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0));
        }
        return new(balance, income, expense, categories, months);
    }
}


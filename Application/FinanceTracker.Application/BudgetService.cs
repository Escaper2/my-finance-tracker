using FinanceTracker.Domain;
namespace FinanceTracker.Application;
public sealed class BudgetService(IFinanceDbFactory factory)
{
    public async Task<List<BudgetView>> List(DateOnly month)
    {
        await using var db = await factory.OpenAsync();
        return Views(db, FinanceRules.MonthOf(month));
    }
    private static List<BudgetView> Views(IFinanceDb db, DateOnly? month = null)
    {
        var budgets = db.Query<Budget>();
        if (month.HasValue) budgets = budgets.Where(b => b.Month == month.Value);
        var categories = db.Query<Category>().ToDictionary(c => c.Id);
        return budgets.ToList().Select(b =>
        {
            var end = b.Month.AddMonths(1);
            var spent = db.Query<Transaction>().Where(t => t.CategoryId == b.CategoryId &&
                t.Type == TransactionType.Expense && t.Date >= b.Month && t.Date < end)
                .Sum(t => (decimal?)t.Amount) ?? 0;
            var category = categories[b.CategoryId];
            return new BudgetView(b.Id, b.CategoryId, category.Name, category.Color, b.Month, b.Limit, spent);
        }).OrderByDescending(b => b.Percent).ToList();
    }
    public void Evaluate(IFinanceDb db)
    {
        foreach (var budget in Views(db))
        {
            var threshold = budget.Percent >= 100 ? 100 : budget.Percent >= 90 ? 90 : 0;
            if (threshold == 0 || db.Query<Notification>().Any(n => n.BudgetId == budget.Id && n.Threshold >= threshold)) continue;
            var text = threshold == 90
                ? $"Бюджет «{budget.Category}» использован на {budget.Percent:0}%. Осталось {budget.Remaining:N2} ₽."
                : budget.Spent == budget.Limit
                    ? $"Лимит бюджета «{budget.Category}» достигнут."
                    : $"Бюджет «{budget.Category}» превышен на {-budget.Remaining:N2} ₽.";
            db.Add(new Notification { BudgetId = budget.Id, Threshold = threshold,
                Message = $"{budget.Month:MM.yyyy} · {text}" });
        }
    }
    public static Task Save(IFinanceDb db, Guid? id, Guid categoryId, DateOnly month, decimal limit)
    {
        FinanceRules.Money(limit);
        var category = db.Query<Category>().Single(c => c.Id == categoryId);
        FinanceRules.Require(category.Type == TransactionType.Expense && !category.IsArchived, "Выберите активную категорию расходов.");
        month = FinanceRules.MonthOf(month);
        FinanceRules.Require(!db.Query<Budget>().Any(b => b.CategoryId == categoryId && b.Month == month && b.Id != id),
            "Для этой категории и месяца бюджет уже существует.");
        var budget = id.HasValue ? db.Query<Budget>().Single(b => b.Id == id.Value) : new Budget();
        // A monthly budget keeps its identity so its alert thresholds cannot accidentally reset.
        FinanceRules.Require(!id.HasValue || (budget.CategoryId == categoryId && budget.Month == month),
            "У существующего бюджета можно изменить только лимит.");
        budget.CategoryId = categoryId; budget.Month = month; budget.Limit = limit;
        if (!id.HasValue) db.Add(budget);
        return Task.CompletedTask;
    }
}


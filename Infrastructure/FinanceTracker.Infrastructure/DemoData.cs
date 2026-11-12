using FinanceTracker.Domain;
namespace FinanceTracker.Infrastructure;
public static class DemoData
{
    public static async Task Seed(FinanceDbContext db)
    {
        var account = new Account { Name = "Основная карта", InitialBalance = 12500 };
        var cash = new Account { Name = "Наличные", InitialBalance = 7000 };
        db.Add(account); db.Add(cash);
        await db.SaveChangesAsync();
        var categories = db.Query<Category>().ToDictionary(c => c.Name);
        var month = FinanceRules.MonthOf(DateOnly.FromDateTime(DateTime.Now));
        foreach (var (name, limit) in new[] { ("Продукты", 20000m), ("Кафе", 8000m), ("Транспорт", 5000m), ("Покупки", 15000m) })
            db.Add(new Budget { CategoryId = categories[name].Id, Month = month, Limit = limit });
        for (var offset = -2; offset <= 0; offset++)
        {
            var start = month.AddMonths(offset);
            db.Add(new Transaction { AccountId = account.Id, CategoryId = categories["Зарплата"].Id,
                Date = start, Amount = 85000, Type = TransactionType.Income, Description = "Зарплата · демонстрационная операция" });
            foreach (var (name, amount, day, text) in new[] {
                ("Продукты", 6450m, 2, "Продукты на неделю"), ("Кафе", 2180m, 3, "Ужин с друзьями"),
                ("Транспорт", 1800m, 4, "Проездной"), ("Жильё", 28000m, 5, "Аренда квартиры"),
                ("Продукты", 7200m, 6, "Покупки в супермаркете"), ("Кафе", 2450m, 7, "Обед и кофе"),
                ("Покупки", 4900m, 8, "Книги и товары для дома"), ("Здоровье", 1500m, 9, "Аптека") })
                db.Add(new Transaction { AccountId = account.Id, CategoryId = categories[name].Id,
                    Date = start.AddDays(day - 1), Amount = amount + (offset == -1 ? 200 : 0),
                    Type = TransactionType.Expense, Description = text });
        }
        await db.SaveChangesAsync();
    }
}


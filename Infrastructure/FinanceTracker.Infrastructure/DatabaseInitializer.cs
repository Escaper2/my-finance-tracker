using FinanceTracker.Domain;
using Microsoft.EntityFrameworkCore;
namespace FinanceTracker.Infrastructure;
public static class DatabaseInitializer
{
    public static async Task Initialize(IDbContextFactory<FinanceDbContext> factory, bool demo)
    {
        await using var db = await factory.CreateDbContextAsync();
        // Initial schema; Will upgraded.
        await db.Database.EnsureCreatedAsync();
        if (!await db.Query<Category>().AnyAsync())
        {
            foreach (var (name, icon, color) in new[] {
                ("Продукты", "◈", "#52796f"), ("Кафе", "☕", "#b77951"), ("Транспорт", "↗", "#6987a4"),
                ("Жильё", "⌂", "#a08cba"), ("Здоровье", "+", "#bf7d83"), ("Покупки", "◇", "#b29b5f") })
                db.Add(new Category { Name = name, Icon = icon, Color = color, Type = TransactionType.Expense });
            db.Add(new Category { Name = "Зарплата", Icon = "↙", Color = "#52796f", Type = TransactionType.Income });
            await db.SaveChangesAsync();
            if (demo) await DemoData.Seed(db);
        }
    }
}


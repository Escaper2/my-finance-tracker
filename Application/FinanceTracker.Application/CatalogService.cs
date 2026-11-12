using FinanceTracker.Domain;
namespace FinanceTracker.Application;

public sealed class CatalogService(IFinanceDbFactory factory, MutationRunner mutations)
{
    public async Task<List<AccountView>> Accounts()
    {
        await using var db = await factory.OpenAsync();
        return db.Query<Account>().OrderBy(a => a.Name).ToList().Select(a =>
        {
            var income = db.Query<Transaction>().Where(t => t.AccountId == a.Id && t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0;
            var expense = db.Query<Transaction>().Where(t => t.AccountId == a.Id && t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0;
            return new AccountView(a.Id, a.Name, a.InitialBalance + income - expense, a.IsArchived, a.InitialBalance);
        }).ToList();
    }
    public async Task<List<Category>> Categories()
    {
        await using var db = await factory.OpenAsync();
        return db.Query<Category>().OrderBy(c => c.Type).ThenBy(c => c.Name).ToList();
    }
    public Task SaveAccount(Guid? id, string name, decimal initial) => mutations.Run(db =>
    {
        name = FinanceRules.Name(name); FinanceRules.Money(initial, false);
        FinanceRules.Require(!db.Query<Account>().Any(a => a.Name == name && a.Id != id), "Счёт с таким названием уже существует.");
        var a = id.HasValue ? db.Query<Account>().Single(a => a.Id == id) : new Account();
        a.Name = name; a.InitialBalance = initial;
        if (!id.HasValue) db.Add(a);
        return Task.CompletedTask;
    }, false);
    public Task SaveCategory(Guid? id, string name, TransactionType type, string color, string icon) => mutations.Run(db =>
    {
        name = FinanceRules.Name(name);
        FinanceRules.Require(Enum.IsDefined(type), "Некорректный тип категории.");
        FinanceRules.Require(System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"), "Некорректный цвет.");
        FinanceRules.Require(!db.Query<Category>().Any(c => c.Name == name && c.Type == type && c.Id != id), "Такая категория уже существует.");
        var c = id.HasValue ? db.Query<Category>().Single(c => c.Id == id) : new Category();
        FinanceRules.Require(!id.HasValue || c.Type == type, "Тип существующей категории менять нельзя.");
        c.Name = name; c.Type = type; c.Color = color; c.Icon = string.IsNullOrWhiteSpace(icon) ? "●" : icon[..Math.Min(icon.Length, 8)];
        if (!id.HasValue) db.Add(c);
        return Task.CompletedTask;
    }, false);
    public Task ArchiveAccount(Guid id, bool value) => mutations.Run(db =>
    {
        FinanceRules.Require(!value || !db.Query<ImportJob>().Any(j => j.AccountId == id &&
            (j.Status == ImportStatus.Queued || j.Status == ImportStatus.Processing)), "Дождитесь завершения импорта.");
        db.Query<Account>().Single(a => a.Id == id).IsArchived = value;
        return Task.CompletedTask;
    }, false);
    public Task ArchiveCategory(Guid id, bool value) => mutations.Run(db =>
    {
        db.Query<Category>().Single(c => c.Id == id).IsArchived = value;
        return Task.CompletedTask;
    }, false);
}


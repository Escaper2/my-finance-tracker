using FinanceTracker.Domain;
namespace FinanceTracker.Application;
public sealed class TransactionService(IFinanceDbFactory factory, MutationRunner mutations)
{
    public async Task<TransactionPage> List(TransactionFilter filter)
    {
        await using var db = await factory.OpenAsync();
        var q = db.Query<Transaction>();
        if (filter.From.HasValue) q = q.Where(t => t.Date >= filter.From.Value);
        if (filter.To.HasValue) q = q.Where(t => t.Date <= filter.To.Value);
        if (filter.AccountId.HasValue) q = q.Where(t => t.AccountId == filter.AccountId);
        if (filter.CategoryId.HasValue) q = q.Where(t => t.CategoryId == filter.CategoryId);
        if (filter.Type.HasValue) q = q.Where(t => t.Type == filter.Type);
        if (filter.Uncategorized) q = q.Where(t => t.CategoryId == null);
        if (!string.IsNullOrWhiteSpace(filter.Search)) q = q.Where(t => t.Description.ToLower().Contains(filter.Search.ToLower()));
        var total = q.Count();
        q = filter.Sort switch {
            "amount-desc" => q.OrderByDescending(t => t.Amount).ThenByDescending(t => t.Date).ThenBy(t => t.Id),
            "date-asc" => q.OrderBy(t => t.Date).ThenBy(t => t.Id),
            _ => q.OrderByDescending(t => t.Date).ThenBy(t => t.Id)
        };
        var accounts = db.Query<Account>().ToDictionary(a => a.Id);
        var categories = db.Query<Category>().ToDictionary(c => c.Id);
        var items = q.Skip((Math.Max(1, filter.Page) - 1) * filter.PageSize).Take(filter.PageSize).ToList()
            .Select(t => {
                var c = t.CategoryId.HasValue ? categories.GetValueOrDefault(t.CategoryId.Value) : null;
                return new TransactionView(t.Id, t.Date, accounts[t.AccountId].Name, c?.Name ?? "Без категории",
                    c?.Icon ?? "○", c?.Color ?? "#8b9690", t.Amount, t.Type, t.Description);
            }).ToList();
        return new(items, total);
    }
    public async Task<Transaction> Get(Guid id)
    {
        await using var db = await factory.OpenAsync();
        return db.Query<Transaction>().Single(t => t.Id == id);
    }
    public Task Save(Transaction input) => mutations.Run(db =>
    {
        FinanceRules.Money(input.Amount);
        FinanceRules.Require(Enum.IsDefined(input.Type), "Некорректный тип операции.");
        FinanceRules.Require(input.Date != default && input.Description.Length <= 500, "Укажите дату и комментарий до 500 символов.");
        var t = db.Query<Transaction>().SingleOrDefault(t => t.Id == input.Id);
        var account = db.Query<Account>().SingleOrDefault(a => a.Id == input.AccountId);
        FinanceRules.Require(account != null && (!account.IsArchived || t?.AccountId == account.Id), "Выберите активный счёт.");
        if (input.CategoryId.HasValue)
        {
            var c = db.Query<Category>().SingleOrDefault(c => c.Id == input.CategoryId.Value);
            FinanceRules.Require(c != null && c.Type == input.Type && (!c.IsArchived || t?.CategoryId == c.Id), "Выберите подходящую категорию.");
        }
        var isNew = t == null; t ??= new Transaction { Id = input.Id };
        t.AccountId = input.AccountId; t.CategoryId = input.CategoryId; t.Date = input.Date;
        t.Amount = input.Amount; t.Type = input.Type; t.Description = input.Description.Trim();
        if (isNew) db.Add(t);
        return Task.CompletedTask;
    });
    public Task Delete(Guid id) => mutations.Run(db =>
    {
        db.Remove(db.Query<Transaction>().Single(t => t.Id == id));
        return Task.CompletedTask;
    });
}


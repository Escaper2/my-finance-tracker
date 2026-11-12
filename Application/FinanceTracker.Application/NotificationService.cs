using FinanceTracker.Domain;
namespace FinanceTracker.Application;
public sealed class NotificationService(IFinanceDbFactory factory, MutationRunner mutations)
{
    public async Task<List<Notification>> List()
    {
        await using var db = await factory.OpenAsync();
        return db.Query<Notification>().OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAt).Take(100).ToList();
    }
    public Task MarkRead(Guid? id = null) => mutations.Run(db =>
    {
        foreach (var n in db.Query<Notification>().Where(n => !n.IsRead && (!id.HasValue || n.Id == id)).ToList())
            n.IsRead = true;
        return Task.CompletedTask;
    }, false);
}


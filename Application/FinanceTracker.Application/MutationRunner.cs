namespace FinanceTracker.Application;
public sealed class MutationRunner(IFinanceDbFactory factory, BudgetService budgets, ChangeBus changes)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task Run(Func<IFinanceDb, Task> action, bool checkBudgets = true)
    {
        await gate.WaitAsync();
        try
        {
            await using var db = await factory.OpenAsync();
            await db.BeginAsync();
            await action(db);
            await db.SaveAsync();
            if (checkBudgets) budgets.Evaluate(db);
            await db.SaveAsync();
            await db.CommitAsync();
        }
        finally { gate.Release(); }
        changes.Publish();
    }
}


namespace FinanceTracker.Application;

// One local application process, one owner. Subscribers marshal updates onto their Blazor circuit.
public sealed class ChangeBus
{
    public event Action? Changed;
    public void Publish()
    {
        foreach (Action handler in Changed?.GetInvocationList() ?? [])
        {
            try { handler(); }
            catch (ObjectDisposedException) { /* A browser circuit may have just disconnected. */ }
        }
    }
}


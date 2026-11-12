using FinanceTracker.Application;
using Microsoft.AspNetCore.Components;
namespace FinanceTracker.Web.Components.Shared;
public abstract class FinancePage : ComponentBase, IDisposable
{
    [Inject] protected ChangeBus Changes { get; set; } = default!;
    protected string? Error;
    protected bool Busy;
    private bool disposed;
    protected override async Task OnInitializedAsync()
    {
        Changes.Changed += Changed;
        await ReloadSafe();
    }
    protected abstract Task Refresh();
    protected async Task ReloadSafe()
    {
        try { await Refresh(); }
        catch (Exception ex) { Error = ex is InvalidOperationException ? ex.Message : "Не удалось загрузить данные. Проверьте подключение к базе."; }
    }
    private void Changed() { _ = InvokeAsync(async () => { if (disposed) return; await ReloadSafe(); StateHasChanged(); }); }
    protected async Task<bool> Execute(Func<Task> action)
    {
        if (Busy) return false;
        Busy = true; Error = null;
        try { await action(); await Refresh(); return true; }
        catch (Exception ex) { Error = ex is InvalidOperationException ? ex.Message : "Не удалось сохранить изменения. Проверьте данные и повторите попытку."; return false; }
        finally { Busy = false; }
    }
    public void Dispose() { disposed = true; Changes.Changed -= Changed; GC.SuppressFinalize(this); }
}


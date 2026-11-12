using FinanceTracker.Application;
using FinanceTracker.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace FinanceTracker.Infrastructure;
public sealed class ImportWorker(IFinanceDbFactory factory, ImportService imports, MutationRunner mutations,
    ILogger<ImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield startup; jobs survive restarts because their content and state are persisted.
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid? id = null;
            try
            {
                await using (var db = await factory.OpenAsync())
                    id = db.Query<ImportJob>().Where(j => j.Status == ImportStatus.Queued || j.Status == ImportStatus.Processing)
                        .OrderBy(j => j.CreatedAt).Select(j => (Guid?)j.Id).FirstOrDefault();
                if (id.HasValue)
                {
                    await imports.Process(id.Value);
                    continue;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Import processing failed for {JobId}", id);
                if (id.HasValue)
                {
                    try
                    {
                        await mutations.Run(db =>
                        {
                            var job = db.Query<ImportJob>().Single(j => j.Id == id);
                            job.Status = ImportStatus.Failed;
                            job.Failure = "Не удалось завершить импорт. Проверьте файл и повторите попытку.";
                            return Task.CompletedTask;
                        }, false);
                    }
                    catch (Exception saveError) { logger.LogError(saveError, "Could not persist import failure"); }
                }
            }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}


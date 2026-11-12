using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Domain;
namespace FinanceTracker.Application;
public sealed class ImportService(IFinanceDbFactory factory, MutationRunner mutations, ICsvParser parser)
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public CsvPreview Preview(string content)
    {
        FinanceRules.Require(Encoding.UTF8.GetByteCount(content) <= MaxBytes, "Максимальный размер CSV — 2 МБ.");
        var preview = parser.Parse(content);
        FinanceRules.Require(preview.Total <= 20000, "В файле допускается не более 20 000 строк.");
        return preview;
    }
    public async Task<Guid> Queue(Guid accountId, string fileName, string content)
    {
        var preview = Preview(content);
        FinanceRules.Require(preview.Rows.Count > 0, "В файле нет корректных операций.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var id = Guid.NewGuid();
        await mutations.Run(db =>
        {
            FinanceRules.Require(db.Query<Account>().Any(a => a.Id == accountId && !a.IsArchived), "Выберите активный счёт.");
            FinanceRules.Require(!db.Query<ImportJob>().Any(j => j.AccountId == accountId && j.FileHash == hash),
                "Этот файл уже загружен для выбранного счёта. Результат доступен в истории.");
            db.Add(new ImportJob { Id = id, AccountId = accountId, FileName = Path.GetFileName(fileName),
                Content = content, FileHash = hash, TotalRows = preview.Total });
            return Task.CompletedTask;
        }, false);
        return id;
    }
    public async Task<List<ImportJob>> History()
    {
        await using var db = await factory.OpenAsync();
        return db.Query<ImportJob>().OrderByDescending(j => j.CreatedAt).Take(30)
            .Select(j => new ImportJob { Id = j.Id, AccountId = j.AccountId, FileName = j.FileName, Status = j.Status,
                CreatedAt = j.CreatedAt, TotalRows = j.TotalRows, ImportedRows = j.ImportedRows,
                DuplicateRows = j.DuplicateRows, ErrorRows = j.ErrorRows, Failure = j.Failure }).ToList();
    }
    public async Task<List<ImportRowError>> Errors(Guid id)
    {
        await using var db = await factory.OpenAsync();
        return db.Query<ImportRowError>().Where(e => e.ImportJobId == id).OrderBy(e => e.RowNumber).Take(200).ToList();
    }
    public Task Retry(Guid id) => mutations.Run(db =>
    {
        var job = db.Query<ImportJob>().Single(j => j.Id == id);
        FinanceRules.Require(job.Status == ImportStatus.Failed, "Повторить можно только неудачный импорт.");
        job.Status = ImportStatus.Queued; job.Failure = null;
        return Task.CompletedTask;
    }, false);
    public async Task Process(Guid id)
    {
        var completed = false;
        await mutations.Run(db => {
            var job = db.Query<ImportJob>().Single(j => j.Id == id);
            completed = job.Status == ImportStatus.Completed;
            if (!completed) job.Status = ImportStatus.Processing;
            return Task.CompletedTask;
        }, false);
        if (completed) return;
        // One atomic batch: after a restart Processing jobs can safely be replayed.
        await mutations.Run(db =>
        {
            var job = db.Query<ImportJob>().Single(j => j.Id == id);
            var preview = Preview(job.Content);
            var categories = db.Query<Category>().Where(c => !c.IsArchived).ToList();
            var knownIds = db.Query<Transaction>().Where(t => t.AccountId == job.AccountId && t.SourceKey != null)
                .Select(t => t.SourceKey!).ToHashSet(StringComparer.Ordinal);
            var imported = 0; var duplicates = 0;
            foreach (var row in preview.Rows)
            {
                if (row.BankId != null && !knownIds.Add(row.BankId)) { duplicates++; continue; }
                var category = categories.FirstOrDefault(c => c.Type == row.Type &&
                    string.Equals(c.Name, row.Category, StringComparison.OrdinalIgnoreCase));
                db.Add(new Transaction { AccountId = job.AccountId, CategoryId = category?.Id, Date = row.Date,
                    Amount = row.Amount, Type = row.Type, Description = row.Description,
                    ImportJobId = id, ImportRow = row.Number, SourceKey = row.BankId });
                imported++;
            }
            foreach (var error in preview.Errors)
                db.Add(new ImportRowError { ImportJobId = id, RowNumber = error.Number, Message = error.Message });
            job.ImportedRows = imported; job.DuplicateRows = duplicates; job.ErrorRows = preview.Errors.Count;
            job.Status = ImportStatus.Completed; job.Failure = null;
            return Task.CompletedTask;
        });
    }
}


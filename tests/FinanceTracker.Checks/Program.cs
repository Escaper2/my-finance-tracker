using FinanceTracker.Application;
using FinanceTracker.Domain;
using FinanceTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var root = Path.Combine(Path.GetTempPath(), "finflow-checks-" + Guid.NewGuid());
Directory.CreateDirectory(root);
var configValues = new Dictionary<string, string?>();
if (Environment.GetEnvironmentVariable("FINANCE_TEST_POSTGRES") is { Length: > 0 } postgres)
{
    configValues["Database:Provider"] = "Postgres";
    configValues["ConnectionStrings:Finance"] = postgres;
}
var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
var services = new ServiceCollection().AddLogging().AddFinance(config, root);
await using var provider = services.BuildServiceProvider();
var dbFactory = provider.GetRequiredService<IDbContextFactory<FinanceDbContext>>();
await DatabaseInitializer.Initialize(dbFactory, false);
await using (var guardDb = await dbFactory.CreateDbContextAsync())
    if (guardDb.Query<Account>().Any()) throw new InvalidOperationException("Checks require a fresh empty database. Never point FINANCE_TEST_POSTGRES at personal data.");
var catalog = provider.GetRequiredService<CatalogService>();
var transactions = provider.GetRequiredService<TransactionService>();
var budgets = provider.GetRequiredService<BudgetService>();
var notifications = provider.GetRequiredService<NotificationService>();
var imports = provider.GetRequiredService<ImportService>();
var reports = provider.GetRequiredService<ReportService>();
var mutations = provider.GetRequiredService<MutationRunner>();
var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); passed++;
}
async Task Reject(Func<Task> action, string name)
{
    try { await action(); } catch (InvalidOperationException) { Check(true, name); return; }
    throw new Exception("FAIL: accepted invalid input: " + name);
}

await catalog.SaveAccount(null, "Test account " + Guid.NewGuid(), 1000.25m);
var account = (await catalog.Accounts()).Last(a => a.Name.StartsWith("Test account "));
var food = (await catalog.Categories()).Single(c => c.Name == "Продукты");
var cafe = (await catalog.Categories()).Single(c => c.Name == "Кафе");
var incomeCategory = (await catalog.Categories()).Single(c => c.Name == "Зарплата");
var month = new DateOnly(2026, 11, 1);
await mutations.Run(db => BudgetService.Save(db, null, food.Id, month, 1000m));
await mutations.Run(db => BudgetService.Save(db, null, cafe.Id, month, 500m));
var transaction = new Transaction { AccountId = account.Id, CategoryId = food.Id, Date = month, Amount = 899.99m, Description = "Check expense" };
await transactions.Save(transaction);
Check((await notifications.List()).Count == 0, "No notification below 90%");
Check((await catalog.Accounts()).Single(a => a.Id == account.Id).Balance == 100.26m, "Exact kopecks in balance");
transaction.Amount = 900m;
await transactions.Save(transaction);
Check((await notifications.List()).Count(n => n.Threshold == 90) == 1, "90% threshold on edit");
transaction.Amount = 950m;
await transactions.Save(transaction);
Check((await notifications.List()).Count == 1, "No repeated threshold warning");
transaction.Amount = 1100.01m;
await transactions.Save(transaction);
Check((await notifications.List()).Count(n => n.Threshold == 100) == 1, "100% threshold on edit");
var report = await reports.Get(month, month.AddMonths(1).AddDays(-1), account.Id);
Check(report.Expense == 1100.01m && report.Balance == -99.76m, "SQL report aggregates exact amounts");
Check(report.Categories.Single().Value == 1100.01m, "SQL category aggregation");
transaction.CategoryId = cafe.Id;
await transactions.Save(transaction);
var items = await budgets.List(month);
Check(items.Single(b => b.CategoryId == food.Id).Spent == 0 && items.Single(b => b.CategoryId == cafe.Id).Spent == 1100.01m, "Category change recalculates both budgets");
Check((await notifications.List()).Count(n => n.Message.Contains("Кафе")) == 1, "Jump over thresholds creates only highest warning");
transaction.Date = month.AddMonths(1);
await transactions.Save(transaction);
Check((await budgets.List(month)).All(b => b.Spent == 0), "Month boundary excludes next month");
await transactions.Delete(transaction.Id);
Check((await catalog.Accounts()).Single(a => a.Id == account.Id).Balance == 1000.25m, "Delete restores balance");
await Reject(() => transactions.Save(new Transaction { AccountId = account.Id, CategoryId = food.Id, Date = month, Amount = -1m }), "Reject negative amount");
await Reject(() => transactions.Save(new Transaction { AccountId = account.Id, CategoryId = incomeCategory.Id, Date = month, Amount = 1m }), "Reject wrong category type");
await Reject(() => mutations.Run(db => BudgetService.Save(db, null, food.Id, month, 10m)), "Reject duplicate monthly budget");
await Reject(() => mutations.Run(db => BudgetService.Save(db, null, food.Id, month.AddMonths(1), 0m)), "Reject zero limit");
await Reject(() => transactions.Save(new Transaction { AccountId = account.Id, Date = month, Amount = 1.001m }), "Reject fractions of a kopeck");
const string header = "date;amount;type;description;category;bank_id\n";
var csv = header + "2026-11-03;450,25;expense;\"Coffee; with dessert\";Продукты;bank-1\n"
    + "2026-11-04;100.10;income;Salary;Зарплата;bank-2\n"
    + "invalid;5;expense;Bad row;Продукты;bank-3\n"
    + "2026-11-05;49.75;expense;Unknown category;Unknown;\n";
var preview = imports.Preview(csv);
Check(preview.Rows.Count == 3 && preview.Errors.Count == 1 && preview.Rows[0].Description == "Coffee; with dessert", "CSV quotes, decimal comma and invalid row");
await Reject(() => Task.Run(() => imports.Preview("date,amount\n2026-01-01,3")), "Reject unsupported CSV header");
var jobId = await imports.Queue(account.Id, "check.csv", csv);
await imports.Process(jobId);
var job = (await imports.History()).Single(j => j.Id == jobId);
Check(job.Status == ImportStatus.Completed && job.ImportedRows == 3 && job.ErrorRows == 1, "Import persists valid rows and row errors");
Check((await imports.Errors(jobId)).Single().RowNumber == 4, "CSV error keeps original line number");
await Reject(() => imports.Queue(account.Id, "renamed.csv", csv), "Reject same file with different name");
await imports.Process(jobId);
Check((await transactions.List(new() { AccountId = account.Id })).Total == 3, "Completed job replay is idempotent");
var nextId = await imports.Queue(account.Id, "overlap.csv", header + "2026-11-03;450.25;expense;Same bank ID;Продукты;bank-1\n2026-11-07;450;expense;New expense;Продукты;bank-4\n");
await imports.Process(nextId);
Check((await imports.History()).Single(j => j.Id == nextId).DuplicateRows == 1, "Overlapping files deduplicate by account and bank ID");
Check((await budgets.List(month)).Single(b => b.CategoryId == food.Id).Spent == 900.25m, "CSV updates budgets");
var uncat = await transactions.List(new() { AccountId = account.Id, Uncategorized = true });
Check(uncat.Total == 1, "Unknown CSV category remains unassigned");
var uncategorized = await transactions.Get(uncat.Items[0].Id);
uncategorized.CategoryId = food.Id;
await transactions.Save(uncategorized);
Check((await budgets.List(month)).Single(b => b.CategoryId == food.Id).Spent == 950m, "Assigning imported category updates budget");
await notifications.MarkRead();
Check((await notifications.List()).All(n => n.IsRead), "Read notification state persists");
var sorted = await transactions.List(new() { AccountId = account.Id, Sort = "amount-desc", PageSize = 2 });
Check(sorted.Items.Count == 2 && sorted.Items[0].Amount >= sorted.Items[1].Amount, "SQL sorting and pagination");
var filtered = await transactions.List(new() { AccountId = account.Id, Search = "salary", Type = TransactionType.Income });
Check(filtered.Total == 1, "Search and type filtering");
await catalog.ArchiveAccount(account.Id, true);
await Reject(() => transactions.Save(new Transaction { AccountId = account.Id, Date = month, Amount = 1m }), "Reject new operations on archived account");
await catalog.ArchiveAccount(account.Id, false);

// Emulate a crash after the worker marked a persisted job Processing.
var recoverId = await imports.Queue(account.Id, "recover.csv", header + "2026-11-08;50;expense;Recovery;Продукты;bank-recovery\n");
await mutations.Run(db => { db.Query<ImportJob>().Single(j => j.Id == recoverId).Status = ImportStatus.Processing; return Task.CompletedTask; }, false);
var worker = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().Single();
await worker.StartAsync(CancellationToken.None);
var deadline = DateTime.UtcNow.AddSeconds(10);
while ((await imports.History()).Single(j => j.Id == recoverId).Status != ImportStatus.Completed && DateTime.UtcNow < deadline)
    await Task.Delay(100);
await worker.StopAsync(CancellationToken.None);
Check((await imports.History()).Single(j => j.Id == recoverId).Status == ImportStatus.Completed, "Worker recovers persisted Processing job");
var snapshot = (await catalog.Accounts()).Single(a => a.Id == account.Id).Balance;
await using (var reopened = new ServiceCollection().AddLogging().AddFinance(config, root).BuildServiceProvider())
    Check((await reopened.GetRequiredService<CatalogService>().Accounts()).Single(a => a.Id == account.Id).Balance == snapshot, "Data survives new service provider");
Console.WriteLine($"\n{passed} checks passed. Database: {root}");
// Test DB is retained in the OS temp directory for failure diagnostics.


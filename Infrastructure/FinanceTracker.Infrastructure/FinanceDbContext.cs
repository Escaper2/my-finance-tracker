using FinanceTracker.Application;
using FinanceTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace FinanceTracker.Infrastructure;
public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options), IFinanceDb
{
    private IDbContextTransaction? transaction;
    public IQueryable<T> Query<T>() where T : class => Set<T>();
    public new void Add<T>(T entity) where T : class => Set<T>().Add(entity);
    public new void Remove<T>(T entity) where T : class => Set<T>().Remove(entity);
    public async Task BeginAsync() => transaction = await Database.BeginTransactionAsync();
    public Task SaveAsync() => SaveChangesAsync();
    public Task CommitAsync() => transaction!.CommitAsync();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Account>().HasIndex(a => a.Name).IsUnique();
        model.Entity<Category>().HasIndex(c => new { c.Name, c.Type }).IsUnique();
        model.Entity<Budget>().HasIndex(b => new { b.CategoryId, b.Month }).IsUnique();
        model.Entity<Notification>().HasIndex(n => new { n.BudgetId, n.Threshold }).IsUnique();
        model.Entity<ImportJob>().HasIndex(j => new { j.AccountId, j.FileHash }).IsUnique();
        model.Entity<Transaction>().HasIndex(t => new { t.AccountId, t.SourceKey }).IsUnique();
        model.Entity<Transaction>().HasIndex(t => new { t.ImportJobId, t.ImportRow }).IsUnique();
        model.Entity<Transaction>().HasIndex(t => new { t.CategoryId, t.Date });
        model.Entity<Transaction>().HasIndex(t => new { t.AccountId, t.Date });
        model.Entity<Transaction>().HasOne<Account>().WithMany().HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Transaction>().HasOne<Category>().WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Transaction>().HasOne<ImportJob>().WithMany().HasForeignKey(t => t.ImportJobId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Budget>().HasOne<Category>().WithMany().HasForeignKey(b => b.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Notification>().HasOne<Budget>().WithMany().HasForeignKey(n => n.BudgetId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ImportJob>().HasOne<Account>().WithMany().HasForeignKey(j => j.AccountId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ImportRowError>().HasOne<ImportJob>().WithMany().HasForeignKey(e => e.ImportJobId).OnDelete(DeleteBehavior.Cascade);
        // Store exact kopecks as integers on both providers: no float rounding and sortable on SQLite.
        model.Entity<Account>().Property(a => a.InitialBalance).HasConversion(v => (long)(v * 100), v => v / 100m);
        model.Entity<Transaction>().Property(t => t.Amount).HasConversion(v => (long)(v * 100), v => v / 100m);
        model.Entity<Budget>().Property(b => b.Limit).HasConversion(v => (long)(v * 100), v => v / 100m);
    }
}


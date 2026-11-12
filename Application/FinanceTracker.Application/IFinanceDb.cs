using FinanceTracker.Domain;
namespace FinanceTracker.Application;

// Queries remain server-side until materialized; each operation owns a short-lived session.
public interface IFinanceDb : IAsyncDisposable
{
    IQueryable<T> Query<T>() where T : class;
    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task BeginAsync();
    Task SaveAsync();
    Task CommitAsync();
}
public interface IFinanceDbFactory { Task<IFinanceDb> OpenAsync(); }
public interface ICsvParser { CsvPreview Parse(string content); }


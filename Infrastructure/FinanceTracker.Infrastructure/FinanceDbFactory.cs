using FinanceTracker.Application;
using Microsoft.EntityFrameworkCore;
namespace FinanceTracker.Infrastructure;
public sealed class FinanceDbFactory(IDbContextFactory<FinanceDbContext> factory) : IFinanceDbFactory
{
    public async Task<IFinanceDb> OpenAsync() => await factory.CreateDbContextAsync();
}


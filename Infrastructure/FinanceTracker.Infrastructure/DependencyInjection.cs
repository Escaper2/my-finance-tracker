using FinanceTracker.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace FinanceTracker.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddFinance(this IServiceCollection services, IConfiguration config, string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        var provider = config["Database:Provider"] ?? "Sqlite";
        services.AddDbContextFactory<FinanceDbContext>(options =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(config.GetConnectionString("Finance") ?? throw new InvalidOperationException("ConnectionStrings:Finance is required."));
            else if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(config.GetConnectionString("Finance") ?? $"Data Source={Path.Combine(dataDirectory, "finance.db")}");
            else throw new InvalidOperationException("Database:Provider must be Sqlite or Postgres.");
        });
        services.AddSingleton<IFinanceDbFactory, FinanceDbFactory>();
        services.AddSingleton<ICsvParser, BankCsvParser>();
        services.AddSingleton<ChangeBus>();
        services.AddSingleton<BudgetService>();
        services.AddSingleton<MutationRunner>();
        services.AddSingleton<TransactionService>();
        services.AddSingleton<CatalogService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<ImportService>();
        services.AddHostedService<ImportWorker>();
        return services;
    }
}


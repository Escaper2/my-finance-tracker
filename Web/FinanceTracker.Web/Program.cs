using System.Globalization;
using FinanceTracker.Infrastructure;
using FinanceTracker.Web.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddFinance(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/error", createScopeForErrors: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
await DatabaseInitializer.Initialize(app.Services.GetRequiredService<IDbContextFactory<FinanceDbContext>>(),
    builder.Configuration.GetValue<bool>("Database:SeedDemo"));
app.Run();


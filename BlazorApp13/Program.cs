using Infra.Persistance.Context;
using Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using PurchaseWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();  // ← 元に戻す

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("db")));

builder.Services.AddMudServices();
builder.Services.AddMudMarkdownServices();

// TenantProviderを登録
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// リポジトリの登録
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductBuyRepository, ProductBuyRepository>();
builder.Services.AddScoped<IPurchaseLogRepository, PurchaseLogRepository>();
builder.Services.AddScoped<LMStudioService>();

// AppSettingsの構成を追加
builder.Services.Configure<AppSettings>(builder.Configuration);

// サービスを追加
builder.Services.AddScoped<AppSettingsService>();
builder.Services.AddScoped<AppSettings>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var appSettings = new AppSettings();
    configuration.Bind(appSettings);
    return appSettings;
});

builder.Services.AddScoped<UserState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();  // ← 元に戻す
app.MapFallbackToPage("/_Host");  // ← 元に戻す

app.Run();

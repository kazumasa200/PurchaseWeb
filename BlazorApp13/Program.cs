using Infra.Persistance.Context;
using Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using PurchaseWeb;
using PurchaseWeb.Adapters;
using PurchaseWeb.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("db")));

// テナントプロバイダー（API エンドポイントが X-Tenant-Id ヘッダーから設定）
builder.Services.AddScoped<SharedTenantProvider>();
builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<SharedTenantProvider>());

// Infra リポジトリ（EF Core 実装）
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductBuyRepository, ProductBuyRepository>();
builder.Services.AddScoped<IPurchaseLogRepository, PurchaseLogRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();

// LM Studio サービス（チャット API）
builder.Services.AddScoped<LMStudioService>();

// AppSettings
builder.Services.AddScoped<AppSettings>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var appSettings = new AppSettings();
    configuration.Bind(appSettings);
    return appSettings;
});
builder.Services.AddScoped<AppSettingsService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors();

// Blazor WASM フレームワークファイルと静的ファイルの配信
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Minimal API エンドポイント
app.MapLMStudioEndpoints();
app.MapProductEndpoints();
app.MapProductBuyEndpoints();
app.MapTenantEndpoints();
app.MapPurchaseLogEndpoints();
app.MapAuthEndpoints();
app.MapQrEndpoints();

// すべての未マッチリクエストは index.html へフォールバック（SPA ルーティング）
app.MapFallbackToFile("index.html");

app.Run();

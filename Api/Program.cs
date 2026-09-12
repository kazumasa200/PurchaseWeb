using Infra.Persistance.Context;
using Infra.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PurchaseWeb.Api.Adapters;
using PurchaseWeb.Api.Endpoints;
using PurchaseWeb.Api.Usecases.Auth;
using PurchaseWeb.Api.Usecases.ProductBuy;
using PurchaseWeb.Api.Usecases.Products;
using PurchaseWeb.Api.Usecases.Purchases;
using PurchaseWeb.Api.Usecases.PurchaseLogs;
using PurchaseWeb.Api.Usecases.Qr;
using PurchaseWeb.Api.Usecases.Tenants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("db")));

builder.Services.AddScoped<SharedTenantProvider>();
builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<SharedTenantProvider>());

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductBuyRepository, ProductBuyRepository>();
builder.Services.AddScoped<IPurchaseLogRepository, PurchaseLogRepository>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();

builder.Services.AddScoped<AppSettings>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var s = new AppSettings();
    cfg.Bind(s);
    return s;
});
builder.Services.AddScoped<AppSettingsService>();

builder.Services.AddScoped<IProductUsecase, ProductUsecase>();
builder.Services.AddScoped<IProductBuyUsecase, ProductBuyUsecase>();
builder.Services.AddScoped<IPurchaseLogUsecase, PurchaseLogUsecase>();
builder.Services.AddScoped<ITenantUsecase, TenantUsecase>();
builder.Services.AddScoped<IAuthUsecase, AuthUsecase>();
builder.Services.AddScoped<IQrUsecase, QrUsecase>();
builder.Services.AddScoped<IPurchaseUsecase, PurchaseUsecase>();

// Cookie の暗号化キーは既定だとメモリ上にしか無く、プロセスが再起動するたびに作り直される。
// そのままだと再起動のたびに店員が全員ログアウトするので、パスが指定されていれば永続化する。
// 例: systemd に Environment=DataProtectionKeyPath=/var/lib/purchaseweb/keys
var keyPath = builder.Configuration["DataProtectionKeyPath"];
if (!string.IsNullOrWhiteSpace(keyPath))
{
    Directory.CreateDirectory(keyPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("PurchaseWeb");
}

// 認証: 店員かどうかだけを判定する単一パスワード方式。
// トークンを JS から読める場所に置かないよう、HttpOnly Cookie で持つ。
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "pw_auth";
        options.Cookie.HttpOnly = true;                      // JS から読めない
        options.Cookie.SameSite = SameSiteMode.Strict;       // CSRF 対策
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;

        // API なのでログイン画面へ 302 せず、素直に 401/403 を返す
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// WASM は API と同一オリジンから配信されるので、本番に CORS は要らない。
// 開発時だけ別ポートのデバッグホストを許可する。
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(p => p
            .SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));
}

var app = builder.Build();

// パスワードが未設定のまま本番に出ると、初期値で誰でも店員になれてしまう。
if (!app.Environment.IsDevelopment())
{
    var storePassword = app.Configuration["StorePassword"];
    if (string.IsNullOrWhiteSpace(storePassword) || storePassword == "your_password_here")
        throw new InvalidOperationException(
            "StorePassword が未設定です。環境変数 StorePassword を設定してから起動してください。");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
    app.UseCors();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapProductEndpoints();
app.MapProductBuyEndpoints();
app.MapPurchaseLogEndpoints();
app.MapPurchaseEndpoints();
app.MapTenantEndpoints();
app.MapAuthEndpoints();
app.MapQrEndpoints();

app.MapFallbackToFile("index.html");

app.Run();

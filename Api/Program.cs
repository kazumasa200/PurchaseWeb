using Infra.Persistance.Context;
using Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using PurchaseWeb.Api.Adapters;
using PurchaseWeb.Api.Endpoints;
using PurchaseWeb.Api.Usecases.Auth;
using PurchaseWeb.Api.Usecases.ProductBuy;
using PurchaseWeb.Api.Usecases.Products;
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

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapProductEndpoints();
app.MapProductBuyEndpoints();
app.MapPurchaseLogEndpoints();
app.MapTenantEndpoints();
app.MapAuthEndpoints();
app.MapQrEndpoints();

app.MapFallbackToFile("index.html");

app.Run();

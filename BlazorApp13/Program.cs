using Infra.Persistance.Context;
using Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("db")));

builder.Services.AddMudServices();

// リポジトリの登録
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductBuyRepository, ProductBuyRepository>();
builder.Services.AddScoped<IPurchaseLogRepository, PurchaseLogRepository>();
builder.Services.AddScoped<LMStudioService>();
// AppSettingsの構成を追加
builder.Services.Configure<AppSettings>(
    builder.Configuration);

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
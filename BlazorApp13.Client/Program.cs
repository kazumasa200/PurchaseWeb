using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using PurchaseWeb.Client;
using PurchaseWeb.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ルートコンポーネントの登録
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient (サーバーのベース URL を自動設定)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor
builder.Services.AddMudServices();
builder.Services.AddMudMarkdownServices();

// クライアント側サービス (HttpClient 経由で API を呼ぶ)
builder.Services.AddScoped<ITenantProvider, ClientTenantProvider>();
builder.Services.AddScoped<ILMStudioService, LMStudioApiService>();
builder.Services.AddScoped<IProductRepository, ProductApiService>();
builder.Services.AddScoped<IProductBuyRepository, ProductBuyApiService>();
builder.Services.AddScoped<IPurchaseLogRepository, PurchaseLogApiService>();
builder.Services.AddScoped<ITenantRepository, TenantApiService>();
builder.Services.AddScoped<UserState>();

try
{
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    // WASM 起動失敗時にブラウザのコンソールへ出力 (F12 で確認可能)
    Console.Error.WriteLine($"[WASM] 起動エラー: {ex}");
    throw;
}

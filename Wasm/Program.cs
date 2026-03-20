using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PurchaseWeb.Wasm;
using PurchaseWeb.Wasm.Repositories;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.Login;
using PurchaseWeb.Wasm.Usecases.PreOrder;
using PurchaseWeb.Wasm.Usecases.ProductManage;
using PurchaseWeb.Wasm.Usecases.PurchaseHistory;
using PurchaseWeb.Wasm.Usecases.Register;
using PurchaseWeb.Wasm.Usecases.TenantManage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

builder.Services.AddScoped<ITenantProvider, ClientTenantProvider>();
builder.Services.AddScoped<UserState>();

builder.Services.AddScoped<IProductRepository, ProductApiRepository>();
builder.Services.AddScoped<IProductBuyRepository, ProductBuyApiRepository>();
builder.Services.AddScoped<IPurchaseLogRepository, PurchaseLogApiRepository>();
builder.Services.AddScoped<ITenantRepository, TenantApiRepository>();
builder.Services.AddScoped<IAuthRepository, AuthApiRepository>();
builder.Services.AddScoped<IQrRepository, QrApiRepository>();

builder.Services.AddScoped<ILoginUsecase, LoginUsecase>();
builder.Services.AddScoped<IRegisterUsecase, RegisterUsecase>();
builder.Services.AddScoped<IProductManageUsecase, ProductManageUsecase>();
builder.Services.AddScoped<IPurchaseHistoryUsecase, PurchaseHistoryUsecase>();
builder.Services.AddScoped<IPreOrderUsecase, PreOrderUsecase>();
builder.Services.AddScoped<ITenantManageUsecase, TenantManageUsecase>();

try
{
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[WASM] 起動エラー: {ex}");
    throw;
}

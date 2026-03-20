using Microsoft.AspNetCore.Components;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.Login;

namespace PurchaseWeb.Wasm.Pages;

public partial class Login : IDisposable
{
    [Inject]
    public required ILoginUsecase LoginUsecase { get; set; }

    [Inject]
    public required UserState UserState { get; set; }

    [Inject]
    public required ITenantProvider TenantProvider { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    private string password = string.Empty;
    private string? selectedTenantId;
    private List<Tenant> tenants = new();
    private bool _initialized = false;

    protected override void OnInitialized()
    {
        UserState.OnStateChanged += OnUserStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            await UserState.InitializeAsync();

            if (UserState.HasTenant && !string.IsNullOrEmpty(UserState.CurrentTenantId))
            {
                TenantProvider.SetTenantId(UserState.CurrentTenantId);
            }

            if (UserState.IsStoreUser && !UserState.HasTenant)
            {
                await LoadTenants();
            }

            _initialized = true;
            StateHasChanged();
        }
    }

    private async void OnUserStateChanged()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"OnUserStateChanged error: {ex.Message}");
        }
    }

    private async Task LoadTenants()
    {
        try
        {
            tenants = await LoginUsecase.GetActiveTenantsAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadTenants error: {ex.Message}");
            tenants = [];
        }
    }

    private async Task Logins()
    {
        var ok = await LoginUsecase.ValidatePasswordAsync(password);
        password = string.Empty;
        await UserState.SetAuthenticatedAsync(ok);

        if (ok)
        {
            await LoadTenants();
            StateHasChanged();
        }
    }

    private async Task SelectTenant()
    {
        if (!string.IsNullOrEmpty(selectedTenantId))
        {
            var tenant = tenants.FirstOrDefault(t => t.TenantId == selectedTenantId);
            if (tenant != null)
            {
                await UserState.SetTenantAsync(tenant.TenantId, tenant.TenantName);
                StateHasChanged();
            }
        }
    }

    private void NavigateToProductManage() => NavigationManager.NavigateTo("/productmanage");
    private void NavigateToRegister() => NavigationManager.NavigateTo("/register");
    private void NavigateToPurchaseHistory() => NavigationManager.NavigateTo("/purchasehistory");
    private void NavigateToTenantManage() => NavigationManager.NavigateTo("/tenantmanage");

    public void Dispose()
    {
        UserState.OnStateChanged -= OnUserStateChanged;
    }
}

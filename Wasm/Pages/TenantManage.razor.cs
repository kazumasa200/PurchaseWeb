using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.TenantManage;

namespace PurchaseWeb.Wasm.Pages;

public partial class TenantManage : IDisposable
{
    [Inject]
    public required ITenantManageUsecase TenantManageUsecase { get; set; }

    [Inject]
    public required UserState UserState { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    private List<Tenant> tenants = new();
    private bool isAddDialogOpen = false;
    private bool isEditDialogOpen = false;
    private bool isDeleteDialogOpen = false;
    private Tenant? selectedTenant;
    private string tenantName = string.Empty;

    protected override void OnInitialized()
    {
        UserState.OnStateChanged += OnUserStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && UserState.IsStoreUser)
        {
            await LoadTenants();
            StateHasChanged();
        }
    }

    private async void OnUserStateChanged()
    {
        try
        {
            await LoadTenants();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"OnUserStateChanged error: {ex.Message}");
        }
    }

    private async Task LoadTenants()
    {
        var all = await TenantManageUsecase.GetAllTenantsAsync();
        tenants = all.Where(t => !t.DeleteFlag).ToList();
        StateHasChanged();
    }

    private void OpenAddDialog()
    {
        tenantName = string.Empty;
        isAddDialogOpen = true;
    }

    private void OpenEditDialog(Tenant tenant)
    {
        selectedTenant = tenant;
        tenantName = tenant.TenantName;
        isEditDialogOpen = true;
    }

    private void OpenDeleteDialog(Tenant tenant)
    {
        selectedTenant = tenant;
        isDeleteDialogOpen = true;
    }

    private async Task AddTenant()
    {
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            Snackbar.Add("テナント名を入力してください", Severity.Warning);
            return;
        }

        var result = await TenantManageUsecase.CreateTenantAsync(tenantName);

        if (result.IsSuccess)
        {
            Snackbar.Add("テナントを追加しました", Severity.Success);
            isAddDialogOpen = false;
            await LoadTenants();
            UserState.NotifyStateChanged();
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? "エラーが発生しました", Severity.Error);
        }
    }

    private async Task UpdateTenant()
    {
        if (selectedTenant == null || string.IsNullOrWhiteSpace(tenantName))
        {
            Snackbar.Add("テナント名を入力してください", Severity.Warning);
            return;
        }

        var updated = new Tenant
        {
            TenantId = selectedTenant.TenantId,
            TenantName = tenantName,
            CreateDate = selectedTenant.CreateDate,
            UpdateDate = DateTime.Now,
            DeleteFlag = selectedTenant.DeleteFlag
        };

        var result = await TenantManageUsecase.UpdateTenantAsync(updated);

        if (result.IsSuccess)
        {
            Snackbar.Add("テナントを更新しました", Severity.Success);
            isEditDialogOpen = false;
            await LoadTenants();
            UserState.NotifyStateChanged();
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? "エラーが発生しました", Severity.Error);
        }
    }

    private async Task DeleteTenant()
    {
        if (selectedTenant == null) return;

        var result = await TenantManageUsecase.DeleteTenantAsync(selectedTenant);

        if (result.IsSuccess)
        {
            Snackbar.Add("テナントを削除しました", Severity.Success);
            isDeleteDialogOpen = false;
            await LoadTenants();
            UserState.NotifyStateChanged();
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? "エラーが発生しました", Severity.Error);
        }
    }

    private void NavigateToLogin() => NavigationManager.NavigateTo("/login");

    public void Dispose()
    {
        UserState.OnStateChanged -= OnUserStateChanged;
    }
}

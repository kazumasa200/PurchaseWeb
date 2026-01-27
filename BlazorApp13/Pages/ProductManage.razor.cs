using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Services;

namespace PurchaseWeb.Pages;

public partial class ProductManage : IDisposable
{
    [Inject]
    public required IProductRepository ProductRepository { get; set; }

    [Inject]
    public required UserState UserState { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required ITenantProvider TenantProvider { get; set; }

    public string NewProdMisc { get; set; } = string.Empty;
    public string NewProdName { get; set; } = string.Empty;
    public int NewProdPrice { get; set; }

    public List<Product> Products { get; set; } = [];
    private bool _initialized = false;
    private bool _isLoading = true;

    public bool IsLoading => _isLoading;

    protected override void OnInitialized()
    {
        UserState.OnStateChanged += OnUserStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _isLoading = true;
            StateHasChanged();

            // 初期化待機
            await Task.Delay(100);

            if (!UserState.IsStoreUser || !UserState.HasTenant)
            {
                _isLoading = false;
                _initialized = true;
                StateHasChanged();
                return;
            }

            if (!string.IsNullOrEmpty(UserState.CurrentTenantId))
            {
                TenantProvider.SetTenantId(UserState.CurrentTenantId);
                await LoadProducts();
            }

            _isLoading = false;
            _initialized = true;
            StateHasChanged();
        }
    }

    private async void OnUserStateChanged()
    {
        _isLoading = true;
        await InvokeAsync(StateHasChanged);

        if (UserState.HasTenant && !string.IsNullOrEmpty(UserState.CurrentTenantId))
        {
            TenantProvider.SetTenantId(UserState.CurrentTenantId);
            await LoadProducts();
        }

        _isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadProducts()
    {
        try
        {
            Products = await ProductRepository.GetActiveProducts();
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Warning);
            Products = [];
        }
    }

    public async Task InsertProduct(string name, int price, string? misc)
    {
        var prod = Product.Create(null, name, price, misc);
        var ret = await ProductRepository.AddAsync(prod);

        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("追加成功", Severity.Success);
            ResetForm();
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("追加失敗", Severity.Error);
        }
        await LoadProducts();
        StateHasChanged();
    }

    public async Task UpdateProduct(Product product)
    {
        var updated = Product.Update(product);
        var ret = await ProductRepository.UpdateAsync(updated);

        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("更新成功", Severity.Success);
            ResetForm();
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("更新失敗", Severity.Error);
        }
        await LoadProducts();
        StateHasChanged();
    }

    public async Task DeleteProduct(Product product)
    {
        var updated = Product.Delete(product);
        var ret = await ProductRepository.UpdateAsync(updated);

        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("更新成功", Severity.Success);
            ResetForm();
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("更新失敗", Severity.Error);
        }
        await LoadProducts();
        StateHasChanged();
    }

    public void ResetForm()
    {
        NewProdMisc = string.Empty;
        NewProdName = string.Empty;
        NewProdPrice = 0;
    }

    private void NavigateToLogin()
    {
        NavigationManager.NavigateTo("/login");
    }

    public void Dispose()
    {
        UserState.OnStateChanged -= OnUserStateChanged;
    }
}

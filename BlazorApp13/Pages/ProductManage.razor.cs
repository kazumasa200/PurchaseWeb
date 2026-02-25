using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using PurchaseWeb.Component;
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

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    // 削除確認ダイアログ用
    private Product? DeleteProductItem { get; set; }

    public string NewProdMisc { get; set; } = string.Empty;
    public string NewProdName { get; set; } = string.Empty;
    public int NewProdPrice { get; set; }
    public int? NewProdStock { get; set; }
    public string? NewProdImageBase64 { get; set; }

    // 検索用
    public string SearchText { get; set; } = string.Empty;
    public List<Product> FilteredProducts => Products
        .Where(p => p.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .ToList();

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

    // 新規作成ダイアログを開く
    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters
        {
            { nameof(ProductEditDialog.IsNew), true }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<ProductEditDialog>("新規商品登録", parameters, options);
        var result = await dialog.Result;

        // ダイアログ内で DB 操作が完了しているので、リストを再読み込みするだけ
        if (result is DialogResult { Data: bool success } && success)
        {
            await LoadProducts();
            StateHasChanged();
        }
    }

    // 編集ダイアログを開く
    private async Task OpenEditDialog(Product product)
    {
        var parameters = new DialogParameters
        {
            { nameof(ProductEditDialog.IsNew), false },
            { nameof(ProductEditDialog.Product), product }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<ProductEditDialog>("商品編集", parameters, options);
        var result = await dialog.Result;

        // ダイアログ内で DB 操作が完了しているので、リストを再読み込みするだけ
        if (result is DialogResult { Data: bool success } && success)
        {
            await LoadProducts();
            StateHasChanged();
        }
    }

    // 削除確認ダイアログを開く
    private async Task ConfirmDelete(Product product)
    {
        var parameters = new DialogParameters
        {
            { nameof(Product), product }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<DeleteConfirmDialog>("商品削除の確認", parameters, options);
        var result = await dialog.Result;

        if (result is DialogResult { Data: bool confirm } && confirm)
        {
            await DeleteProduct(product);
        }
    }

    public async Task InsertProduct(Product product)
    {
        var ret = await ProductRepository.AddAsync(product);

        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("追加成功", Severity.Success);
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
            Snackbar.Add("削除成功", Severity.Success);
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("削除失敗", Severity.Error);
        }
        await LoadProducts();
        StateHasChanged();
    }

    private string GetStockText(int? stock)
    {
        if (stock == null) return "無制限";
        if (stock == 0) return "在庫なし";
        return $"{stock}個";
    }

    private Color GetStockColor(int? stock)
    {
        if (stock == null) return Color.Default;
        if (stock == 0) return Color.Error;
        if (stock <= 5) return Color.Warning;
        return Color.Success;
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
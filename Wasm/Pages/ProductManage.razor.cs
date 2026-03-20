using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.ProductManage;

namespace PurchaseWeb.Wasm.Pages;

public partial class ProductManage : IDisposable
{
    [Inject]
    public required IProductManageUsecase ProductManageUsecase { get; set; }

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

    public string SearchText { get; set; } = string.Empty;

    public List<Product> FilteredProducts => Products
        .Where(p => p.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .ToList();

    public List<Product> Products { get; set; } = [];
    private bool _initialized = false;
    private bool _isLoading = true;
    public bool IsLoading => _isLoading;

    private Dictionary<string, string?> _productImages = [];
    private Dictionary<string, string?> _productImageSrcs = [];
    private HashSet<string> _imageLoadComplete = [];

    protected override void OnInitialized()
    {
        UserState.OnStateChanged += OnUserStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            try
            {
                _isLoading = true;
                StateHasChanged();
                await Task.Delay(100);

                if (UserState.IsStoreUser && UserState.HasTenant &&
                    !string.IsNullOrEmpty(UserState.CurrentTenantId))
                {
                    TenantProvider.SetTenantId(UserState.CurrentTenantId);
                    await LoadProducts();
                    _ = LoadImagesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ProductManage OnAfterRenderAsync error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                _initialized = true;
                StateHasChanged();
            }
        }
    }

    private async void OnUserStateChanged()
    {
        try
        {
            _isLoading = true;
            await InvokeAsync(StateHasChanged);

            if (UserState.HasTenant && !string.IsNullOrEmpty(UserState.CurrentTenantId))
            {
                TenantProvider.SetTenantId(UserState.CurrentTenantId);
                await LoadProducts();
                _ = LoadImagesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"OnUserStateChanged error: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadProducts()
    {
        try
        {
            Products = await ProductManageUsecase.GetProductsAsync();
            _productImages = [];
            _productImageSrcs = [];
            _imageLoadComplete = [];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadProducts error: {ex.Message}");
            Snackbar.Add("商品の読み込みに失敗しました", Severity.Warning);
            Products = [];
        }
    }

    private async Task LoadImagesAsync()
    {
        try
        {
            var allImages = await ProductManageUsecase.GetAllProductImagesAsync();
            foreach (var (productId, base64) in allImages)
            {
                _productImages[productId] = base64;
                _productImageSrcs[productId] = base64 is null ? null : $"data:image;base64,{base64}";
                ImageCache.Set(productId, base64);
                _imageLoadComplete.Add(productId);
            }
            foreach (var product in Products)
            {
                var id = product.ProductId;
                if (!_imageLoadComplete.Contains(id) && ImageCache.TryGet(id, out var cached))
                {
                    _productImages[id] = cached;
                    _productImageSrcs[id] = cached is null ? null : $"data:image;base64,{cached}";
                    _imageLoadComplete.Add(id);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadImagesAsync error: {ex.Message}");
        }
        await InvokeAsync(StateHasChanged);
    }

    public bool IsImageLoaded(string productId) => _imageLoadComplete.Contains(productId);
    public string? GetProductImage(string productId) => _productImages.GetValueOrDefault(productId);
    public string? GetProductImageSrc(string productId) => _productImageSrcs.GetValueOrDefault(productId);

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters
        {
            { nameof(Component.ProductEditDialog.IsNew), true }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<Component.ProductEditDialog>("新規商品登録", parameters, options);
        var result = await dialog.Result;

        if (result is DialogResult { Data: bool success } && success)
        {
            await LoadProducts();
            _ = LoadImagesAsync();
            StateHasChanged();
        }
    }

    private async Task OpenEditDialog(Product product)
    {
        var parameters = new DialogParameters
        {
            { nameof(Component.ProductEditDialog.IsNew), false },
            { nameof(Component.ProductEditDialog.Product), product }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<Component.ProductEditDialog>("商品編集", parameters, options);
        var result = await dialog.Result;

        if (result is DialogResult { Data: bool success } && success)
        {
            ImageCache.Remove(product.ProductId);
            await LoadProducts();
            _ = LoadImagesAsync();
            StateHasChanged();
        }
    }

    private async Task ConfirmDelete(Product product)
    {
        var parameters = new DialogParameters
        {
            { nameof(Component.DeleteConfirmDialog.Product), product }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<Component.DeleteConfirmDialog>("商品削除の確認", parameters, options);
        var result = await dialog.Result;

        if (result is DialogResult { Data: bool confirm } && confirm)
        {
            await DeleteProduct(product);
        }
    }

    public async Task DeleteProduct(Product product)
    {
        var ret = await ProductManageUsecase.DeleteProductAsync(product.ProductId);

        if (ret.IsSuccess)
            Snackbar.Add("削除成功", Severity.Success);
        else
            Snackbar.Add(ret.ErrorMessage ?? "削除失敗", Severity.Error);

        await LoadProducts();
        _ = LoadImagesAsync();
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

    private void NavigateToLogin() => NavigationManager.NavigateTo("/login");

    public void Dispose()
    {
        UserState.OnStateChanged -= OnUserStateChanged;
    }
}

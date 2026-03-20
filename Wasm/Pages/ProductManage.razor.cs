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
        var productIds = Products.Select(p => p.ProductId).ToList();
        foreach (var id in productIds)
        {
            try
            {
                string? base64;
                if (ImageCache.TryGet(id, out var cached))
                {
                    base64 = cached;
                }
                else
                {
                    base64 = await ProductManageUsecase.GetProductImageAsync(id);
                    ImageCache.Set(id, base64);
                }
                _productImages[id] = base64;
                _productImageSrcs[id] = base64 is null ? null : $"data:image;base64,{base64}";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"LoadImagesAsync [{id}] error: {ex.Message}");
                _productImages[id] = null;
                _productImageSrcs[id] = null;
            }
            _imageLoadComplete.Add(id);
            await InvokeAsync(StateHasChanged);
        }
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
        // 画像がまだロード中の場合はここで取得してからダイアログを開く（nullのまま保存すると画像が消える）
        if (IsImageLoaded(product.ProductId))
        {
            product.ImageBase64 = GetProductImage(product.ProductId);
        }
        else
        {
            try
            {
                var img = await ProductManageUsecase.GetProductImageAsync(product.ProductId);
                _productImages[product.ProductId] = img;
                _productImageSrcs[product.ProductId] = img is null ? null : $"data:image;base64,{img}";
                ImageCache.Set(product.ProductId, img);
                _imageLoadComplete.Add(product.ProductId);
                product.ImageBase64 = img;
            }
            catch
            {
                product.ImageBase64 = null;
            }
        }

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

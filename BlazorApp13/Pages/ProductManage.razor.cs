using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
    public int? NewProdStock { get; set; }
    public string? NewProdImageBase64 { get; set; }
    public string? EditingProductId { get; set; }
    public bool IsEditing => !string.IsNullOrEmpty(EditingProductId);
    public bool IsFormExpanded { get; set; } = false;

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

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file != null)
        {
            // 最大 5MB
            var maxFileSize = 5 * 1024 * 1024;

            if (file.Size > maxFileSize)
            {
                Snackbar.Add("ファイルサイズは 5MB 以下にしてください", Severity.Error);
                return;
            }

            try
            {
                // Base64 エンコードして保存
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                NewProdImageBase64 = Convert.ToBase64String(memoryStream.ToArray());

                Snackbar.Add("画像をアップロードしました", Severity.Success);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"画像アップロードエラー：{ex.Message}", Severity.Error);
                NewProdImageBase64 = null;
            }
        }
    }

    private void ClearImage()
    {
        NewProdImageBase64 = null;
    }

    private void EditProduct(Product product)
    {
        EditingProductId = product.ProductId;
        NewProdName = product.ProductName;
        NewProdPrice = product.Price;
        NewProdStock = product.StockQuantity;
        NewProdMisc = product.Misc ?? string.Empty;
        NewProdImageBase64 = product.ImageBase64;
        IsFormExpanded = true;
        StateHasChanged();
    }

    private void CancelEdit()
    {
        ResetForm();
    }

    private async Task SaveProduct()
    {
        if (IsEditing)
        {
            await UpdateExistingProduct();
        }
        else
        {
            await InsertProduct(NewProdName, NewProdPrice, NewProdMisc, NewProdImageBase64, NewProdStock);
        }
    }

    public async Task InsertProduct(string name, int price, string? misc, string? imageBase64, int? stockQuantity)
    {
        var prod = Product.Create(null, name, price, misc, imageBase64, stockQuantity);
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

    private async Task UpdateExistingProduct()
    {
        var existingProduct = Products.FirstOrDefault(p => p.ProductId == EditingProductId);
        if (existingProduct == null)
        {
            Snackbar.Add("商品が見つかりません", Severity.Error);
            return;
        }

        // 一時的に public setter でプロパティを更新（後でドメインモデルを拡張可能）
        var updatedProduct = Product.Create(
            existingProduct.ProductId,
            NewProdName,
            NewProdPrice,
            NewProdMisc,
            NewProdImageBase64,
            NewProdStock
        );

        // CreateDate を維持
        typeof(Product).GetProperty("CreateDate")!.SetValue(updatedProduct, existingProduct.CreateDate);
        typeof(Product).GetProperty("DeleteFlag")!.SetValue(updatedProduct, existingProduct.DeleteFlag);

        var finalProduct = Product.Update(updatedProduct);
        var ret = await ProductRepository.UpdateAsync(finalProduct);

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
            Snackbar.Add("削除成功", Severity.Success);
            ResetForm();
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

    public void ResetForm()
    {
        NewProdMisc = string.Empty;
        NewProdName = string.Empty;
        NewProdPrice = 0;
        NewProdStock = null;
        NewProdImageBase64 = null;
        EditingProductId = null;
        IsFormExpanded = false;
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
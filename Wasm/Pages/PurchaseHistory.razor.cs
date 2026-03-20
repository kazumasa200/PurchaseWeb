using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.PurchaseHistory;

namespace PurchaseWeb.Wasm.Pages;

public partial class PurchaseHistory : IDisposable
{
    [Inject]
    public required IPurchaseHistoryUsecase PurchaseHistoryUsecase { get; set; }

    [Inject]
    public required ITenantProvider TenantProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required UserState UserState { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    public List<PurchaseLog> PurchaseLogs { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<ProductSummary> ProductSummaries { get; set; } = [];

    private bool _initialized = false;
    private bool _isLoading = true;
    private bool _isDeleteDialogOpen = false;
    private PurchaseLog? _selectedLog;

    public bool IsLoading => _isLoading;

    public class ProductSummary
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Price { get; set; }
        public int TotalAmount { get; set; }
        public int TotalSales { get; set; }
        public bool IsDeleted { get; set; }
    }

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
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"PurchaseHistory OnAfterRenderAsync error: {ex.Message}");
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
                await LoadData();
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

    private async Task LoadData()
    {
        try
        {
            (PurchaseLogs, Products) = await PurchaseHistoryUsecase.LoadDataAsync();
            CalculateSummaries();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadData error: {ex.Message}");
            Snackbar.Add("データの読み込みに失敗しました", Severity.Warning);
            PurchaseLogs = [];
            Products = [];
            ProductSummaries = [];
        }
    }

    private void CalculateSummaries()
    {
        ProductSummaries = Products.Select(product =>
        {
            var productLogs = PurchaseLogs.Where(log => log.ProductId == product.ProductId).ToList();
            var totalAmount = productLogs.Sum(log => log.Amount);
            var totalSales = totalAmount * product.Price;

            return new ProductSummary
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                TotalAmount = totalAmount,
                TotalSales = totalSales,
                IsDeleted = product.DeleteFlag
            };
        })
        .OrderByDescending(s => s.TotalSales)
        .ToList();
    }

    private void OpenDeleteDialog(PurchaseLog log)
    {
        _selectedLog = log;
        _isDeleteDialogOpen = true;
    }

    private async Task DeletePurchase()
    {
        if (_selectedLog == null) return;

        var ret = await PurchaseHistoryUsecase.DeleteLogAsync(_selectedLog.LogId);

        if (ret.IsSuccess)
        {
            Snackbar.Add("削除成功", Severity.Success);
            _isDeleteDialogOpen = false;
            await LoadData();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(ret.ErrorMessage ?? "削除失敗", Severity.Error);
        }
    }

    private string GetProductName(string productId)
    {
        var product = Products.FirstOrDefault(p => p.ProductId == productId);
        return product?.ProductName ?? "不明な商品";
    }

    private int GetProductPrice(string productId)
    {
        var product = Products.FirstOrDefault(p => p.ProductId == productId);
        return product?.Price ?? 0;
    }

    private void NavigateToLogin() => NavigationManager.NavigateTo("/login");

    public void Dispose()
    {
        UserState.OnStateChanged -= OnUserStateChanged;
    }
}

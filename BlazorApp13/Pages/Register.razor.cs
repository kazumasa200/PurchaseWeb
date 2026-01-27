using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Services;

namespace PurchaseWeb.Pages;

public partial class Register : IDisposable
{
    public required ApplicationDbContext DbFactory { get; set; }

    [Inject]
    public required IProductBuyRepository ProductBuyRepository { get; set; }

    [Inject]
    public required IPurchaseLogRepository PurchaseLogRepository { get; set; }

    [Inject]
    public required ITenantProvider TenantProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required UserState UserState { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    public List<ProductBuy> Products { get; set; } = [];
    public int Received { get; set; }

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
                await GetProducts();
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
            await GetProducts();
        }

        _isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    public async Task GetProducts()
    {
        try
        {
            Products = await ProductBuyRepository.GetActiveProductBuy();
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Warning);
            Products = [];
        }
    }

    public async Task Purchase()
    {
        if (!UserState.HasTenant)
        {
            Snackbar.Add("テナントが選択されていません", Severity.Error);
            return;
        }

        var data = new List<PurchaseLog>();
        foreach (var product in Products)
        {
            if (product.Amount > 0)
            {
                data.Add(PurchaseLog.Create(product.Amount, product.Product.ProductId, null));
            }
        }

        if (data.Count > 0)
        {
            var ret = await PurchaseLogRepository.AddRangeAsync(data);
            if (ret != null && ret.IsSuccess)
            {
                Snackbar.Add("お買い上げありがとうございます！", Severity.Success);
                Received = 0;
            }
            else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
            {
                Snackbar.Add(ret.ErrorMessage, Severity.Error);
            }
            else
            {
                Snackbar.Add("何らかの問題が発生しました。購買スタッフまでお声がけください。", Severity.Error);
            }
            await GetProducts();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("購入情報を入力してください。");
        }
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

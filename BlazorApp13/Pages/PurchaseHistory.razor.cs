using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class PurchaseHistory
{
    [Inject]
    public required IPurchaseLogRepository PurchaseLogRepository { get; set; }

    [Inject]
    public required IProductRepository ProductRepository { get; set; }

    public List<Product> Products { get; set; } = [];

    public List<PurchaseLog> PurchaseLogs { get; set; } = [];

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    /// <summary>
    /// 購買情報削除
    /// </summary>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="misc"></param>
    public async void DeletePurchase(PurchaseLog target)
    {
        var data = PurchaseLog.Delete(target);
        var ret = await PurchaseLogRepository.DeleteAsync(data);
        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("削除しました。", Severity.Success);
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("削除に失敗しました。", Severity.Error);
        }

        await GetProducts();
        StateHasChanged();
    }

    /// <summary>
    /// 製品情報を取得する
    /// </summary>
    public async Task GetProducts()
    {
        Products = await ProductRepository.GetActiveProducts();
        PurchaseLogs = await PurchaseLogRepository.GetPurchaseLogsAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await GetProducts();
    }
}
using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class Register
{
    public required ApplicationDbContext DbFactory { get; set; }

    [Inject]
    public required IProductBuyRepository ProductBuyRepository { get; set; }

    [Inject]
    public required IPurchaseLogRepository PurchaseLogRepository { get; set; }

    public List<ProductBuy> Products { get; set; } = [];

    public List<Product> ProductsOrig { get; set; } = [];

    public int Received { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    public async Task GetProducts()
    {
        Products = await ProductBuyRepository.GetActiveProductBuy();
    }

    /// <summary>
    /// 購買情報挿入
    /// </summary>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="misc"></param>
    public async Task Purchase()
    {
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

    protected override async Task OnInitializedAsync()
    {
        await GetProducts();
    }
}
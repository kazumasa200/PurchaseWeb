using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class Register
{
    public required ApplicationDbContext DbFactory { get; set; }

    [Inject]
    public required IDbContextFactory<ApplicationDbContext> DBFactory { get; set; }

    [Inject]
    public required IProductBuyRepository ProductBuyRepository { get; set; }

    public List<ProductBuy> Products { get; set; } = [];

    public List<Product> ProductsOrig { get; set; } = [];

    public int Received { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    public async Task GetProducts()
    {
        DbFactory = DBFactory.CreateDbContext();
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
        try
        {
            foreach (var product in Products)
            {
                if (product.Amount > 0)
                {
                    var Purchase = new PurchaseLog()
                    {
                        Amount = product.Amount,
                        DeleteFlag = false,
                        ProductId = product.Product.ProductId,
                        LogId = Guid.NewGuid().ToString(),
                        PurchaseDate = DateTime.Now,
                    };
                    DbFactory.PurchaseLog.Add(Purchase);
                }
            }

            DbFactory.SaveChanges();
            Snackbar.Add("お買い上げありがとうございます！", Severity.Success);
            Received = 0;
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }

        await GetProducts();
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await GetProducts();
    }
}
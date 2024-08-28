using BlazorApp13.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace BlazorApp13.Pages;

public partial class Counter
{
    public ApplicationDbContext DbFactory { get; set; }

    [Inject]
    public IDbContextFactory<ApplicationDbContext> DBFactory { get; set; }

    public string NewProdMisc { get; set; } = string.Empty;

    public string NewProdName { get; set; } = string.Empty;

    public int NewProdPrice { get; set; }

    public List<Product> Products { get; set; } = [];

    public int Received { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; }

    public void GetProducts()
    {
        DbFactory = DBFactory.CreateDbContext();
        Products = [.. DbFactory.Product.Where(x => x.DeleteFlag == false).OrderBy(x => x.CreateDate)];
    }

    /// <summary>
    /// 購買情報挿入
    /// </summary>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="misc"></param>
    public void Purchase()
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
                        ProductId = product.ProductId,
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

        GetProducts();
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        GetProducts();
    }
}

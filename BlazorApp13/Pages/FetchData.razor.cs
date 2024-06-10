using PurchaseWeb.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class FetchData
{
    [Inject]
    public required IDbContextFactory<ApplicationDbContext> DBFactory { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    public List<Product> Products { get; set; } = [];

    public List<PurchaseLog> PurchaseLogs { get; set; } = [];

    public required ApplicationDbContext DbFactory { get; set; }

    public string NewProdName { get; set; } = string.Empty;

    public int NewProdPrice { get; set; }

    public string NewProdMisc { get; set; } = string.Empty;

    public int Received { get; set; }

    public void GetProducts()
    {
        DbFactory = DBFactory.CreateDbContext();
        Products = DbFactory.Product.OrderBy(x => x.CreateDate).ToList();
        PurchaseLogs = DbFactory.PurchaseLog.Where(x => x.DeleteFlag == false).OrderByDescending(x => x.PurchaseDate).ToList();
    }

    protected override void OnInitialized()
    {
        GetProducts();
    }

    /// <summary>
    /// 購買情報挿入
    /// </summary>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="misc"></param>
    public void DeletePurchase(string purchaseId)
    {
        try
        {
            var a = DbFactory.PurchaseLog.Single(x => x.LogId == purchaseId);
            if (a != null)
            {
                a.DeleteFlag = true;
                DbFactory.PurchaseLog.Update(a);
                DbFactory.SaveChanges();
            }

            Snackbar.Add("取り消しました。", Severity.Success);
            Received = 0;
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }

        GetProducts();
        StateHasChanged();
    }
}

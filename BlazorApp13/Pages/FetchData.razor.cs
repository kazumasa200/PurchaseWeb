using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using PurchaseWeb.Data;

namespace PurchaseWeb.Pages;

public partial class FetchData
{
    public ApplicationDbContext DbFactory { get; set; }

    [Inject]
    public IDbContextFactory<ApplicationDbContext> DBFactory { get; set; }

    public string NewProdMisc { get; set; } = string.Empty;

    public string NewProdName { get; set; } = string.Empty;

    public int NewProdPrice { get; set; }

    public List<Product> Products { get; set; } = [];

    public List<PurchaseLog> PurchaseLogs { get; set; } = [];

    public int Received { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; }

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
}

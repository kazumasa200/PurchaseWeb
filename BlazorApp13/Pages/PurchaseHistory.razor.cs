using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class PurchaseHistory
{
    public required ApplicationDbContext DbFactory { get; set; }

    [Inject]
    public required IDbContextFactory<ApplicationDbContext> DBFactory { get; set; }

    public List<Product> Products { get; set; } = [];

    public List<PurchaseLog> PurchaseLogs { get; set; } = [];

    public int Received { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    /// <summary>
    /// 購買情報削除
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

    /// <summary>
    /// 製品情報を取得する
    /// </summary>
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
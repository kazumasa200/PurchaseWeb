using PurchaseWeb.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace PurchaseWeb.Pages;

public partial class Index
{
    [Inject]
    public required IDbContextFactory<ApplicationDbContext> DBFactory { get; set; }
    
    /// <summary>
    /// 　商品のリスト
    /// </summary>
    public List<Product> Products { get; set; } = [];

    public required ApplicationDbContext DbFactory { get; set; } 

    public string NewProdName { get; set; } = string.Empty;

    public int NewProdPrice { get; set; }

    public string NewProdMisc { get; set; } = string.Empty;

    public void GetProducts()
    {
        DbFactory = DBFactory.CreateDbContext();
        Products = DbFactory.Product.Where(x => x.DeleteFlag == false).OrderBy(x => x.CreateDate).ToList();
    }

    protected override void OnInitialized()
    {
        GetProducts();
    }
     
    /// <summary>
    /// 製品追加
    /// </summary>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="misc"></param>
    public void InsertProduct(string name, int price , string? misc)
    {
        var prod = new Product()
        {
            ProductId = Guid.NewGuid().ToString(),
            ProductName = name,
            Price = price,
            Misc = misc,
            CreateDate = DateTime.Now,
            UpdateDate = DateTime.Now,
            DeleteFlag = false,
        };

        DbFactory.Product.Add(prod);
        DbFactory.SaveChanges();

        ResetForm();
        GetProducts();
        StateHasChanged();
    }

    /// <summary>
    /// 製品更新
    /// </summary>
    /// <param name="product"></param>
    public void UpdateProduct(Product product)
    {
        var a = DbFactory.Product.Single(x => x.ProductId == product.ProductId);
        if (a != null)
        {
            a.ProductName = product.ProductName;
            a.Price = product.Price;
            a.Misc = product.Misc;
            DbFactory.Product.Update(a);
            DbFactory.SaveChanges();
        }

        GetProducts();
        StateHasChanged();
    }

    /// <summary>
    /// 製品削除
    /// </summary>
    /// <param name="product"></param>
    public void DeleteProduct(Product product)
    {
        var a = DbFactory.Product.Single(x => x.ProductId == product.ProductId);
        if(a != null)
        {
            a.DeleteFlag = true;
            DbFactory.Product.Update(a);
            DbFactory.SaveChanges();
        }

        GetProducts();
        StateHasChanged();
    }

    /// <summary>
    /// 新規追加のところをリセット
    /// </summary>
    public void ResetForm()
    {
        NewProdMisc = string.Empty;
        NewProdName = string.Empty;
        NewProdPrice = 0;
    }
}

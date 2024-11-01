using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class ProductManage
{
    [Inject]
    public required IProductRepository ProductRepository { get; set; }

    public string NewProdMisc { get; set; } = string.Empty;

    public string NewProdName { get; set; } = string.Empty;

    public int NewProdPrice { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    /// <summary>
    /// 　商品のリスト
    /// </summary>
    public List<Product> Products { get; set; } = [];

    /// <summary>
    /// 製品削除
    /// </summary>
    /// <param name="product"></param>
    public async Task DeleteProduct(Product product)
    {
        var updated = Product.Delete(product);
        var ret = await ProductRepository.UpdateAsync(updated);
        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("更新成功", Severity.Success);
            ResetForm();
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("更新失敗", Severity.Error);
        }
        await GetProducts();
        StateHasChanged();
    }

    public async Task GetProducts()
    {
        Products = await ProductRepository.GetActiveProducts();
    }

    /// <summary>
    /// 製品追加
    /// </summary>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="misc"></param>
    public async Task InsertProduct(string name, int price, string? misc)
    {
        var prod = Product.Create(null, name, price, misc);

        var ret = await ProductRepository.AddAsync(prod);
        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("追加成功", Severity.Success);
            ResetForm();
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("追加失敗", Severity.Error);
        }
        await GetProducts();
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

    /// <summary>
    /// 製品更新
    /// </summary>
    /// <param name="product"></param>
    public async Task UpdateProduct(Product product)
    {
        var updated = Product.Update(product);
        var ret = await ProductRepository.UpdateAsync(updated);
        if (ret != null && ret.IsSuccess)
        {
            Snackbar.Add("更新成功", Severity.Success);
            ResetForm();
        }
        else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
        {
            Snackbar.Add(ret.ErrorMessage, Severity.Error);
        }
        else
        {
            Snackbar.Add("更新失敗", Severity.Error);
        }
        await GetProducts();
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await GetProducts();
    }
}
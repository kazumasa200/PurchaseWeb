namespace Infra.Persistance.Entities;

/// <summary>
/// 商品マスタ
/// </summary>
public class Product
{
    /// <summary>
    /// 作成日
    /// </summary>
    public DateTime CreateDate { get; private set; }

    /// <summary>
    /// 削除フラグ
    /// </summary>
    public bool DeleteFlag { get; private set; }

    /// <summary>
    /// 備考
    /// </summary>
    public string? Misc { get; private set; }

    /// <summary>
    /// 価格
    /// </summary>
    public int Price { get; private set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public string ProductId { get; private set; } = string.Empty;

    /// <summary>
    /// 商品名
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// 更新日
    /// </summary>
    public DateTime? UpdateDate { get; private set; }

    public virtual ICollection<PurchaseLog> PurchaseLogs { get; private set; } = [];

    /// <summary>
    /// モデル作成
    /// </summary>
    public static Product Create(string? id, string name, int price, string? misc)
    {
        // ドメインルールのバリデーション
        if (string.IsNullOrEmpty(name))
            throw new Exception("商品名は必須です");
        if (price < 0)
            throw new Exception("価格は0以上である必要があります");

        var ret = new Product();
        if (id != null)
        {
            ret.ProductId = id;
        }
        else
        {
            ret.ProductId = Guid.NewGuid().ToString();
        }
        ret.ProductName = name;
        ret.Price = price;
        ret.DeleteFlag = false;
        ret.CreateDate = DateTime.Now;
        ret.UpdateDate = DateTime.Now;

        ret.Misc = misc;

        return ret;
    }

    /// <summary>
    /// モデル更新
    /// </summary>
    /// <param name="name"></param>
    /// <param name="price"></param>
    /// <param name="misc"></param>
    /// <param name="original"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Product Update(Product product)
    {
        // ドメインルールのバリデーション
        if (string.IsNullOrEmpty(product.ProductName))
            throw new Exception("商品名は必須です");
        if (product.Price < 0)
            throw new Exception("価格は0以上である必要があります");

        return new Product
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Price = product.Price,
            DeleteFlag = product.DeleteFlag,
            CreateDate = product.CreateDate,
            Misc = product.Misc,
            UpdateDate = DateTime.Now,
        };
    }

    /// <summary>
    /// モデル削除
    /// </summary>
    /// <param name="original"></param>
    /// <returns></returns>
    public static Product Delete(Product original)
    {
        return new Product
        {
            ProductId = original.ProductId,
            ProductName = original.ProductName,
            Price = original.Price,
            DeleteFlag = true,
            CreateDate = original.CreateDate,
            Misc = original.Misc,
            UpdateDate = DateTime.Now,
        };
    }
}
namespace Infra.Persistance.Entities;

/// <summary>
/// 商品マスタ
/// </summary>
public class Product : ITenantEntity
{
    /// <summary>
    /// テナント ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 作成日
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// 削除フラグ
    /// </summary>
    public bool DeleteFlag { get; set; }

    /// <summary>
    /// 備考
    /// </summary>
    public string? Misc { get; set; }

    /// <summary>
    /// 価格
    /// </summary>
    public int Price { get; set; }

    /// <summary>
    /// 商品 ID
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// 商品名
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 更新日
    /// </summary>
    public DateTime? UpdateDate { get; set; }

    /// <summary>
    /// 画像データ（Base64）
    /// </summary>
    public string? ImageBase64 { get; set; }

    /// <summary>
    /// 在庫数（NULL=無制限）
    /// </summary>
    public int? StockQuantity { get; set; }

    public virtual ICollection<PurchaseLog> PurchaseLogs { get; private set; } = [];

    public Tenant? Tenant { get; set; }

    /// <summary>
    /// モデル作成
    /// </summary>
    public static Product Create(string? id, string name, int price, string? misc, string? imageBase64 = null, int? stockQuantity = null)
    {
        // ドメインルールのバリデーション
        if (string.IsNullOrEmpty(name))
            throw new Exception("商品名は必須です");
        if (price < 0)
            throw new Exception("価格は 0 以上である必要があります");
        if (stockQuantity.HasValue && stockQuantity.Value < 0)
            throw new Exception("在庫数は 0 以上である必要があります");

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
        ret.ImageBase64 = imageBase64;
        ret.StockQuantity = stockQuantity;

        return ret;
    }

    /// <summary>
    /// モデル更新
    /// </summary>
    public static Product Update(Product product)
    {
        // ドメインルールのバリデーション
        if (string.IsNullOrEmpty(product.ProductName))
            throw new Exception("商品名は必須です");
        if (product.Price < 0)
            throw new Exception("価格は 0 以上である必要があります");
        if (product.StockQuantity.HasValue && product.StockQuantity.Value < 0)
            throw new Exception("在庫数は 0 以上である必要があります");

        return new Product
        {
            TenantId = product.TenantId,
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Price = product.Price,
            DeleteFlag = product.DeleteFlag,
            CreateDate = product.CreateDate,
            Misc = product.Misc,
            ImageBase64 = product.ImageBase64,
            StockQuantity = product.StockQuantity,
            UpdateDate = DateTime.Now,
        };
    }

    /// <summary>
    /// 在庫を減らす
    /// </summary>
    public static Product ReduceStock(Product product, int quantity)
    {
        if (product.StockQuantity.HasValue)
        {
            var newStock = product.StockQuantity.Value - quantity;

            return new Product
            {
                TenantId = product.TenantId,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                DeleteFlag = product.DeleteFlag,
                CreateDate = product.CreateDate,
                Misc = product.Misc,
                ImageBase64 = product.ImageBase64,
                StockQuantity = newStock,
                UpdateDate = DateTime.Now,
            };
        }

        // 在庫管理なしの場合は変更なし
        return product;
    }

    /// <summary>
    /// モデル削除
    /// </summary>
    public static Product Delete(Product original)
    {
        return new Product
        {
            TenantId = original.TenantId,
            ProductId = original.ProductId,
            ProductName = original.ProductName,
            Price = original.Price,
            DeleteFlag = true,
            CreateDate = original.CreateDate,
            Misc = original.Misc,
            ImageBase64 = original.ImageBase64,
            StockQuantity = original.StockQuantity,
            UpdateDate = DateTime.Now,
        };
    }
}
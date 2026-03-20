namespace Infra.Persistance.Entities;

/// <summary>
/// 商品マスタ（画像は ProductImages テーブルで管理）
/// </summary>
public class Product : ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public bool DeleteFlag { get; set; }
    public string? Misc { get; set; }
    public int Price { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime? UpdateDate { get; set; }
    public int? StockQuantity { get; set; }

    public virtual ICollection<PurchaseLog> PurchaseLogs { get; private set; } = [];
    public Tenant? Tenant { get; set; }

    public static Product Create(string? id, string name, int price, string? misc, int? stockQuantity = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new Exception("商品名は必須です");
        if (price < 0)
            throw new Exception("価格は 0 以上である必要があります");
        if (stockQuantity.HasValue && stockQuantity.Value < 0)
            throw new Exception("在庫数は 0 以上である必要があります");

        return new Product
        {
            ProductId     = id ?? Guid.NewGuid().ToString(),
            ProductName   = name,
            Price         = price,
            DeleteFlag    = false,
            CreateDate    = DateTime.Now,
            UpdateDate    = DateTime.Now,
            Misc          = misc,
            StockQuantity = stockQuantity
        };
    }

    public static Product Update(Product product)
    {
        if (string.IsNullOrEmpty(product.ProductName))
            throw new Exception("商品名は必須です");
        if (product.Price < 0)
            throw new Exception("価格は 0 以上である必要があります");
        if (product.StockQuantity.HasValue && product.StockQuantity.Value < 0)
            throw new Exception("在庫数は 0 以上である必要があります");

        return new Product
        {
            TenantId      = product.TenantId,
            ProductId     = product.ProductId,
            ProductName   = product.ProductName,
            Price         = product.Price,
            DeleteFlag    = product.DeleteFlag,
            CreateDate    = product.CreateDate,
            Misc          = product.Misc,
            StockQuantity = product.StockQuantity,
            UpdateDate    = DateTime.Now,
        };
    }

    public static Product Delete(Product original)
    {
        return new Product
        {
            TenantId      = original.TenantId,
            ProductId     = original.ProductId,
            ProductName   = original.ProductName,
            Price         = original.Price,
            DeleteFlag    = true,
            CreateDate    = original.CreateDate,
            Misc          = original.Misc,
            StockQuantity = original.StockQuantity,
            UpdateDate    = DateTime.Now,
        };
    }

    public static Product ReduceStock(Product product, int quantity)
    {
        if (product.StockQuantity.HasValue)
        {
            return new Product
            {
                TenantId      = product.TenantId,
                ProductId     = product.ProductId,
                ProductName   = product.ProductName,
                Price         = product.Price,
                DeleteFlag    = product.DeleteFlag,
                CreateDate    = product.CreateDate,
                Misc          = product.Misc,
                StockQuantity = product.StockQuantity.Value - quantity,
                UpdateDate    = DateTime.Now,
            };
        }
        return product;
    }
}

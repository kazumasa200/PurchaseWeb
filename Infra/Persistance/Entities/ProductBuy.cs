namespace Infra.Persistance.Entities;

/// <summary>
/// 商品購入
/// </summary>
public class ProductBuy
{
    /// <summary>
    /// 購入数
    /// </summary>
    public int Amount { get; set; } = 0;

    public Product Product { get; private set; } = new Product();

    /// <summary>
    /// 合計金額
    /// </summary>
    public int Sum
    {
        get
        {
            return Product.Price * Amount;
        }
    }

    /// <summary>
    /// 既存のProductからProductBuyを生成するファクトリメソッド
    /// </summary>
    public static ProductBuy CreateFromProduct(Product product)
    {
        return new ProductBuy
        {
            Product = product,
            Amount = 0
        };
    }

    /// <summary>
    /// 購入数を設定
    /// </summary>
    public void SetAmount(int amount)
    {
        if (amount < 0)
            throw new Exception("購入数は0以上である必要があります");
        Amount = amount;
    }
}

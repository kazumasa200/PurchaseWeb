namespace Infra.Persistance.Entities;

/// <summary>
/// 購入履歴
/// </summary>
public class PurchaseLog : ITenantEntity
{
    /// <summary>
    /// 購入数
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// 削除フラグ
    /// </summary>
    public bool DeleteFlag { get; set; }

    /// <summary>
    /// ログID
    /// </summary>
    public string LogId { get; set; } = string.Empty;

    /// <summary>
    /// 商品ID
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// 購入日付
    /// </summary>
    public DateTime PurchaseDate { get; private set; }

    /// <summary>
    /// テナントID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    public Tenant? Tenant { get; set; }
    public virtual Product Product { get; private set; } = null!;

    public static PurchaseLog Create(int amount, string productId, string? id)
    {
        if (string.IsNullOrEmpty(productId))
            throw new Exception("商品IDは必須です");
        if (amount < 1)
            throw new Exception("数量は1以上である必要があります");

        var ret = new PurchaseLog();
        if (!string.IsNullOrWhiteSpace(id))
        {
            ret.LogId = id;
        }
        else
        {
            ret.LogId = Guid.NewGuid().ToString();
        }
        ret.Amount = amount;
        ret.ProductId = productId;
        ret.PurchaseDate = DateTime.Now;
        ret.DeleteFlag = false;

        return ret;
    }

    public static PurchaseLog Delete(PurchaseLog original)
    {
        return new PurchaseLog
        {
            Amount = original.Amount,
            DeleteFlag = true,
            LogId = original.LogId,
            ProductId = original.ProductId,
            PurchaseDate = original.PurchaseDate,
            TenantId = original.TenantId,
        };
    }
}

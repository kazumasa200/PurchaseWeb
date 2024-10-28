namespace Infra.Persistance.Entities;

/// <summary>
/// 購入履歴
/// </summary>
public class PurchaseLog
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

    public DateTime PurchaseDate { get; set; }

    public virtual Product Product { get; private set; } = null!;
}
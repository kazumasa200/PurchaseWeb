using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchaseWeb.Data;

/// <summary>
/// 購入履歴
/// </summary>
[Table("purchase_log")]
public class PurchaseLog
{
    /// <summary>
    /// 購入数
    /// </summary>
    [Column("amount")]
    public int Amount { get; set; }

    /// <summary>
    /// 削除フラグ
    /// </summary>
    [Column("delete_flag")]
    public bool DeleteFlag { get; set; }

    /// <summary>
    /// ログID
    /// </summary>
    [Key]
    [Column("log_id")]
    public string LogId { get; set; } = string.Empty;

    /// <summary>
    /// 商品ID
    /// </summary>
    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// 購入日付
    /// </summary>
    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }
}
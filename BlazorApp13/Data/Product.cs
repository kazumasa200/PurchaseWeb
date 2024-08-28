using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorApp13.Data;

[Table("product")]
public class Product
{
    [NotMapped]
    public int Amount { get; set; } = 0;

    /// <summary>
    /// 作成日
    /// </summary>
    [Column("create_date")]
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// 削除フラグ
    /// </summary>
    [Column("delete_flag")]
    public bool DeleteFlag { get; set; }

    /// <summary>
    /// 備考
    /// </summary>
    [Column("misc")]
    public string? Misc { get; set; }

    /// <summary>
    /// 価格
    /// </summary>
    [Column("price")]
    public int Price { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    [Key]
    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// 商品名
    /// </summary>
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [NotMapped]
    public int Sum
    {
        get
        {
            return Price * Amount;
        }
    }

    /// <summary>
    /// 更新日
    /// </summary>
    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

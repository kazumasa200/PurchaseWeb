using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorApp13.Data;

[Table("purchase_log")]
public class PurchaseLog
{
    [Column("amount")]
    public int Amount { get; set; }

    [Column("delete_flag")]
    public bool DeleteFlag { get; set; }

    [Key]
    [Column("log_id")]
    public string LogId { get; set; } = string.Empty;

    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }
}

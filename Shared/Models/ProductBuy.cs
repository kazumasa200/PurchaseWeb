namespace PurchaseWeb.Client.Models;

public class ProductBuy
{
    public int Amount { get; set; } = 0;
    public Product Product { get; set; } = new();
    public int Sum => Product.Price * Amount;
}

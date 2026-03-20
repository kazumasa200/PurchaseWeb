namespace PurchaseWeb.Api.Usecases.Qr;

public interface IQrUsecase
{
    string GenerateCartQr(string tenantId, List<QrCartItem> items);
    string GenerateUrlQr(string url);
}

public record QrCartItem(string ProductId, int Quantity);

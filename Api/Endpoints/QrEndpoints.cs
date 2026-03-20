using PurchaseWeb.Api.Usecases.Qr;

namespace PurchaseWeb.Api.Endpoints;

public static class QrEndpoints
{
    public static void MapQrEndpoints(this WebApplication app)
    {
        app.MapPost("/api/qr/generate-cart", (QrCartRequest req, IQrUsecase usecase) =>
            Results.Ok(usecase.GenerateCartQr(req.TenantId, req.Items)));

        app.MapPost("/api/qr/generate-url", (QrUrlRequest req, IQrUsecase usecase) =>
            Results.Ok(usecase.GenerateUrlQr(req.Url)));
    }
}

public record QrCartRequest(string TenantId, List<QrCartItem> Items);
public record QrUrlRequest(string Url);

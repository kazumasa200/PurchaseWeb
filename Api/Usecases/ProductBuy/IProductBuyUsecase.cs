namespace PurchaseWeb.Api.Usecases.ProductBuy;

public interface IProductBuyUsecase
{
    Task<List<Infra.Persistance.Entities.ProductBuy>> GetActiveProductBuyWithoutImagesAsync();
}

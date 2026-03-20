using Infra.Repositories;

namespace PurchaseWeb.Api.Usecases.ProductBuy;

public class ProductBuyUsecase : IProductBuyUsecase
{
    private readonly IProductBuyRepository _repo;

    public ProductBuyUsecase(IProductBuyRepository repo)
    {
        _repo = repo;
    }

    public Task<List<Infra.Persistance.Entities.ProductBuy>> GetActiveProductBuyWithoutImagesAsync()
        => _repo.GetActiveProductBuyWithoutImages();
}

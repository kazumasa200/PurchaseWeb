using Infra.Persistance.Entities;
using Infra.Repositories;

namespace PurchaseWeb.Api.Usecases.Purchases;

public interface IPurchaseUsecase
{
    Task<Result<List<PurchaseLog>>> PurchaseAsync(List<PurchaseLog> purchaseLogs);
}

public class PurchaseUsecase : IPurchaseUsecase
{
    private readonly IPurchaseRepository _repository;

    public PurchaseUsecase(IPurchaseRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<List<PurchaseLog>>> PurchaseAsync(List<PurchaseLog> purchaseLogs)
        => _repository.PurchaseAsync(purchaseLogs);
}

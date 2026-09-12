using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.Register;

public class RegisterUsecase : IRegisterUsecase
{
    private readonly IProductBuyRepository _productBuyRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IProductRepository _productRepo;

    public RegisterUsecase(
        IProductBuyRepository productBuyRepo,
        IPurchaseRepository purchaseRepo,
        IProductRepository productRepo)
    {
        _productBuyRepo = productBuyRepo;
        _purchaseRepo   = purchaseRepo;
        _productRepo    = productRepo;
    }

    public Task<List<ProductBuy>> GetProductsAsync()
        => _productBuyRepo.GetActiveProductBuyWithoutImages();

    public Task<string?> GetProductImageAsync(string productId)
        => _productRepo.GetProductImageAsync(productId);

    /// <summary>
    /// 購入を確定する。
    ///
    /// 以前はここで「履歴の追加」→「商品ごとに在庫の減算」を別々の HTTP で呼んでいたため、
    /// 途中でブラウザが落ちると履歴だけ残って在庫が減らなかった（しかも減算の結果を見ていなかった）。
    /// いまは POST /api/purchase の 1 リクエストで、サーバーが 1 トランザクションとして処理する。
    /// </summary>
    public Task<Result<PurchaseLog>> PurchaseAsync(List<PurchaseLog> items)
        => _purchaseRepo.PurchaseAsync(items);
}

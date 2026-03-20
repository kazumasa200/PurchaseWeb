using Infra.Repositories;

namespace PurchaseWeb.Api.Usecases.Auth;

public class AuthUsecase : IAuthUsecase
{
    private readonly AppSettings _settings;

    public AuthUsecase(AppSettings settings)
    {
        _settings = settings;
    }

    public bool ValidatePassword(string password)
        => password == _settings.StorePassword;
}

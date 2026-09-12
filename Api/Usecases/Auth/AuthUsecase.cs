using System.Security.Cryptography;
using System.Text;
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
    {
        var expected = _settings.StorePassword;
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(password))
            return false;

        // 比較時間から文字数や先頭一致が漏れないように固定時間で比べる
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(password),
            Encoding.UTF8.GetBytes(expected));
    }
}

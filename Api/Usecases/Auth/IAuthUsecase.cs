namespace PurchaseWeb.Api.Usecases.Auth;

public interface IAuthUsecase
{
    bool ValidatePassword(string password);
}

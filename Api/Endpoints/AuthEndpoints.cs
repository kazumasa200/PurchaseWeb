using PurchaseWeb.Api.Usecases.Auth;

namespace PurchaseWeb.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/validate", (PasswordRequest req, IAuthUsecase usecase) =>
            usecase.ValidatePassword(req.Password) ? Results.Ok() : Results.Unauthorized());
    }
}

public record PasswordRequest(string Password);

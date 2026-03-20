using Infra.Repositories;

namespace PurchaseWeb.Api;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // POST /api/auth/validate  → パスワード検証
        app.MapPost("/api/auth/validate", (PasswordRequest req, AppSettings settings) =>
        {
            return req.Password == settings.StorePassword
                ? Results.Ok()
                : Results.Unauthorized();
        });
    }
}

public record PasswordRequest(string Password);

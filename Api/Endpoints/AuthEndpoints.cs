using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PurchaseWeb.Api.Usecases.Auth;

namespace PurchaseWeb.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // パスワードが合っていたら HttpOnly Cookie を発行する。
        // クライアントの localStorage は表示用のヒントでしかなく、
        // 実際の可否はこの Cookie を見てサーバーが決める。
        app.MapPost("/api/auth/validate", async (PasswordRequest req, IAuthUsecase usecase, HttpContext ctx) =>
        {
            if (!usecase.ValidatePassword(req.Password))
                return Results.Unauthorized();

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "store"), new Claim(ClaimTypes.Role, StoreRole)],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await ctx.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Results.Ok();
        });

        app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        });

        // 再読み込み後にクライアントが「まだ店員か」を確かめるため
        app.MapGet("/api/auth/me", (HttpContext ctx) =>
            ctx.User.Identity?.IsAuthenticated == true
                ? Results.Ok(new { isStoreUser = true })
                : Results.Unauthorized());
    }

    public const string StoreRole = "store";
}

public record PasswordRequest(string Password);

using System.Net.Http.Json;

namespace PurchaseWeb.Wasm.Repositories;

public class AuthApiRepository : IAuthRepository
{
    private readonly HttpClient _http;

    public AuthApiRepository(HttpClient http) => _http = http;

    /// <summary>パスワードを検証する。成功するとサーバーが HttpOnly Cookie を発行する。</summary>
    public async Task<bool> ValidatePasswordAsync(string password)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/validate", new { password });
        return res.IsSuccessStatusCode;
    }

    /// <summary>いま店員として認証されているかをサーバーに聞く。</summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var res = await _http.GetAsync("/api/auth/me");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>サーバー側の Cookie を失効させる。</summary>
    public async Task LogoutAsync()
        => await _http.PostAsync("/api/auth/logout", null);
}

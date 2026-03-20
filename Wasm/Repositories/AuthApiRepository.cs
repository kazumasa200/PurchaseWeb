using System.Net.Http.Json;

namespace PurchaseWeb.Wasm.Repositories;

public class AuthApiRepository : IAuthRepository
{
    private readonly HttpClient _http;

    public AuthApiRepository(HttpClient http) => _http = http;

    public async Task<bool> ValidatePasswordAsync(string password)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/validate", new { password });
        return res.IsSuccessStatusCode;
    }
}

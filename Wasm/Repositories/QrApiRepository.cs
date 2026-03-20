using System.Net.Http.Json;

namespace PurchaseWeb.Wasm.Repositories;

public class QrApiRepository : IQrRepository
{
    private readonly HttpClient _http;

    public QrApiRepository(HttpClient http) => _http = http;

    public async Task<string?> GenerateCartQrAsync(string tenantId, List<QrCartItem> items)
    {
        var payload = new { tenantId, items };
        var res = await _http.PostAsJsonAsync("/api/qr/generate-cart", payload);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<string>();
    }

    public async Task<string?> GenerateUrlQrAsync(string url)
    {
        var res = await _http.PostAsJsonAsync("/api/qr/generate-url", new { url });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<string>();
    }
}

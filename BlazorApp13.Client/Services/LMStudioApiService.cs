using System.Net.Http.Json;
using System.Text.Json;
using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Client.Services;

/// <summary>
/// WASM から /api/lmstudio プロキシ経由で LM Studio を利用するサービス
/// </summary>
public class LMStudioApiService : ILMStudioService
{
    private readonly HttpClient _http;

    public LMStudioApiService(HttpClient http) => _http = http;

    public async Task<List<LMStudioModel>> GetModelsAsync()
        => await _http.GetFromJsonAsync<List<LMStudioModel>>("/api/lmstudio/models") ?? [];

    public async IAsyncEnumerable<string> StreamMessagesAsync(
        string modelId,
        List<ChatMessage> chatHistory,
        ScrollToBottomContext scrollToBottomContext)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/lmstudio/stream")
        {
            Content = JsonContent.Create(new { ModelId = modelId, Messages = chatHistory })
        };

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:")) continue;

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") break;

            string? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<string>(data);
            }
            catch
            {
                continue;
            }

            if (!string.IsNullOrEmpty(chunk))
            {
                scrollToBottomContext.RequestScrollToBottom();
                yield return chunk;
            }
        }
    }
}

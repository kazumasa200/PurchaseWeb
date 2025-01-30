using Infra.Persistance.Entities;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Infra.Repositories;

public class LMStudioService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://noeleve.net:64792/v1";

    public LMStudioService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    public async Task<List<LMStudioModel>> GetModelsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<JsonDocument>("/v1/models");
        if (response == null)
        {
            return [];
        }
        var models = response.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(x => new LMStudioModel
            {
                Id = x.GetProperty("id")!.GetString()!,
                Name = x.GetProperty("id")!.GetString()!
            })
            .ToList();
        return models;
    }

    public async Task<string> SendMessageAsync(string modelId, List<ChatMessage> chatHistory)
    {
        // OpenAI互換フォーマットに変換
        var messages = chatHistory.Select(msg => new
        {
            role = msg.Role,
            content = msg.Content
        }).ToList();

        var request = new
        {
            model = modelId,
            messages
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request);
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        if (result == null)
        {
            return string.Empty;
        }
        return result.RootElement!
            .GetProperty("choices")[0]!
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()!;
    }

    public async IAsyncEnumerable<string> StreamMessagesAsync(string modelId, List<ChatMessage> chatHistory, ScrollToBottomContext scrollToBottomContext)
    {
        var messages = chatHistory.Select(msg => new
        {
            role = msg.Role,
            content = msg.Content
        }).ToList();

        var request = new
        {
            model = modelId,
            messages = messages,
            stream = true
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // HttpRequestMessageを作成
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = requestContent
        };

        // SendAsyncを使用してリクエストを送信
        var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead
        );

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
            {
                continue;
            }

            var jsonData = line.Substring(5).Trim();
            if (string.IsNullOrEmpty(jsonData))
            {
                continue;
            }

            JsonDocument jsonDoc = null;
            try
            {
                if (jsonData == "[DONE]") { continue; }
                scrollToBottomContext.RequestScrollToBottom();
                jsonDoc = JsonDocument.Parse(jsonData);
            }
            catch (JsonException)
            {
                continue;
            }

            if (jsonDoc?.RootElement.TryGetProperty("choices", out var choices) == true)
            {
                var choice = choices.EnumerateArray().FirstOrDefault();
                if (choice.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var content))
                {
                    var chunk = content.GetString();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        yield return chunk;
                    }
                }
            }
        }
    }
}
using Infra.Persistance.Entities;
using System.Net.Http.Json;
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
        var models = response.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(x => new LMStudioModel
            {
                Id = x.GetProperty("id").GetString(),
                Name = x.GetProperty("id").GetString()
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
            messages = messages
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request);
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return result.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}
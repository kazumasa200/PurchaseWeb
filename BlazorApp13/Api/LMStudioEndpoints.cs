using System.Text.Json;
using Infra.Repositories;
using PurchaseWeb.Client.Models;
using InfraEntities = Infra.Persistance.Entities;

namespace PurchaseWeb.Api;

public static class LMStudioEndpoints
{
    public static void MapLMStudioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lmstudio");

        // モデル一覧
        group.MapGet("models", async (LMStudioService lmStudio) =>
        {
            try
            {
                var models = await lmStudio.GetModelsAsync();
                var dtos = models.Select(m => new LMStudioModel { Id = m.Id, Name = m.Name }).ToList();
                return Results.Ok(dtos);
            }
            catch
            {
                return Results.Ok(Array.Empty<LMStudioModel>());
            }
        });

        // ストリーミングチャット (SSE)
        group.MapPost("stream", async (
            HttpContext ctx,
            ChatStreamRequest req,
            LMStudioService lmStudio) =>
        {
            ctx.Response.ContentType = "text/event-stream; charset=utf-8";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Append("X-Accel-Buffering", "no");

            ctx.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()
                ?.DisableBuffering();

            try
            {
                // Client モデル → Infra モデルへ変換
                var infraMessages = req.Messages
                    .Select(m => new InfraEntities.ChatMessage { Role = m.Role, Content = m.Content })
                    .ToList();

                var scrollCtx = new InfraEntities.ScrollToBottomContext();

                await foreach (var chunk in lmStudio.StreamMessagesAsync(req.ModelId, infraMessages, scrollCtx))
                {
                    var json = JsonSerializer.Serialize(chunk);
                    await ctx.Response.WriteAsync($"data: {json}\n\n");
                    await ctx.Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                var errJson = JsonSerializer.Serialize($"[エラー] {ex.Message}");
                await ctx.Response.WriteAsync($"data: {errJson}\n\n");
                await ctx.Response.Body.FlushAsync();
            }
            finally
            {
                await ctx.Response.WriteAsync("data: [DONE]\n\n");
                await ctx.Response.Body.FlushAsync();
            }
        });
    }
}

record ChatStreamRequest(string ModelId, List<ChatMessage> Messages);

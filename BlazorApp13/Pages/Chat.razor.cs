using Infra.Persistance.Entities;
using Infra.Repositories;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text;

namespace PurchaseWeb.Pages;

public partial class Chat
{
    private List<LMStudioModel> models = [];
    private readonly List<ChatMessage> chatMessages = [];
    private string selectedModel = string.Empty;
    private string currentMessage = string.Empty;
    private string lastChunk = "";
    private string lastMessage = "";
    private bool isThinking;
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private readonly ScrollToBottomContext _scrollToBottomContext = new();

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required LMStudioService LMStudioService { get; set; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    private bool CanSendMessage => !string.IsNullOrEmpty(selectedModel) && !string.IsNullOrEmpty(currentMessage);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            models = await LMStudioService.GetModelsAsync();
            if (models.Count > 0)
            {
                selectedModel = models[0].Id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Snackbar.Add("サーバーがオフラインのようです", Severity.Error);
        }
    }

    private async Task SendMessage()
    {
        if (!CanSendMessage) return;
        _scrollToBottomContext.RequestScrollToBottom();
        // ユーザーメッセージを追加
        var userMessage = new ChatMessage { Role = "user", Content = currentMessage };
        chatMessages.Add(userMessage);

        // 入力フィールドをクリア
        currentMessage = string.Empty;

        // AIの応答メッセージを準備
        var assistantMessage = new ChatMessage
        {
            Role = "assistant",
            Content = string.Empty,
            ThinkingContents = new List<ThinkingContent>()
        };
        chatMessages.Add(assistantMessage);

        var responseBuilder = new StringBuilder();

        try
        {
            await foreach (var chunk in LMStudioService.StreamMessagesAsync(selectedModel, chatMessages, _scrollToBottomContext))
            {
                responseBuilder.Append(chunk);
                assistantMessage.Content = responseBuilder.ToString();

                // think タグのパース処理
                if (assistantMessage.Content.Contains("</think>"))
                {
                    var parsedMessage = ParseAIResponse("assistant", assistantMessage.Content);
                    assistantMessage.ThinkingContents = parsedMessage.ThinkingContents;
                }
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            assistantMessage.Content = $"エラーが発生しました: {ex.Message}";
            await InvokeAsync(StateHasChanged);
        }
    }

    private ChatMessage ParseAIResponse(string role, string content)
    {
        var message = new ChatMessage { Role = role, Content = content };
        var thinkingContents = new List<ThinkingContent>();

        while (true)
        {
            var thinkStartIndex = content.IndexOf("<think>");
            var thinkEndIndex = content.IndexOf("</think>");

            if (thinkStartIndex == -1 || thinkEndIndex == -1) break;

            var beforeThink = content[..thinkStartIndex];
            var thinkText = content.Substring(thinkStartIndex + 7, thinkEndIndex - thinkStartIndex - 7);

            content = content[(thinkEndIndex + 8)..];

            thinkingContents.Add(new ThinkingContent
            {
                BeforeThink = beforeThink,
                ThinkText = thinkText,
                AfterThink = content
            });
        }

        message.ThinkingContents = thinkingContents;
        return message;
    }
}

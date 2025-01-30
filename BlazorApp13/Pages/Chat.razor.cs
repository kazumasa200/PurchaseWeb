using Infra.Persistance.Entities;
using Infra.Repositories;
using Markdig;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class Chat
{
    private List<LMStudioModel> models = [];
    private readonly List<ChatMessage> chatMessages = [];
    private string selectedModel = string.Empty;
    private string currentMessage = string.Empty;
    private bool isThinking;
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required LMStudioService LMStudioService { get; set; }

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

        // ユーザーメッセージを追加
        var userMessage = new ChatMessage { Role = "user", Content = currentMessage };
        chatMessages.Add(userMessage);
        currentMessage = string.Empty;
        isThinking = true;
        try
        {
            // 全チャット履歴を送信
            var response = await LMStudioService.SendMessageAsync(selectedModel, chatMessages);

            // AIの応答をパースして追加
            var assistantMessage = ParseAIResponse("assistant", response);
            chatMessages.Add(assistantMessage);

            // 入力フィールドをクリア
            currentMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Snackbar.Add("エラーが発生しました", Severity.Error);
        }
        isThinking = false;
        StateHasChanged();
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
                BeforeThink = Markdown.ToHtml(beforeThink, pipeline),
                ThinkText = Markdown.ToHtml(thinkText, pipeline),
                AfterThink = Markdown.ToHtml(content, pipeline)
            });
        }

        message.ThinkingContents = thinkingContents;
        return message;
    }
}

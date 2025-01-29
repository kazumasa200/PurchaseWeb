using Infra.Persistance.Entities;
using Markdig;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PurchaseWeb.Pages;

public partial class Chat
{
    private List<LMStudioModel> models = new();
    private List<ChatMessage> chatMessages = new();
    private string selectedModel;
    private string currentMessage;
    private bool isThinking;

    [Inject]
    ISnackbar Snackbar { get; set; }

    private bool CanSendMessage => !string.IsNullOrEmpty(selectedModel) && !string.IsNullOrEmpty(currentMessage);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            models = await LMStudioService.GetModelsAsync();
        }
        catch (Exception ex)
        {
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

            var beforeThink = content.Substring(0, thinkStartIndex);
            var thinkText = content.Substring(thinkStartIndex + 7, thinkEndIndex - thinkStartIndex - 7);

            content = content.Substring(thinkEndIndex + 8);

            thinkingContents.Add(new ThinkingContent
            {
                BeforeThink = Markdown.ToHtml(beforeThink),
                ThinkText = Markdown.ToHtml(thinkText),
                AfterThink = Markdown.ToHtml(content)
            });
        }

        message.ThinkingContents = thinkingContents;
        return message;
    }
}
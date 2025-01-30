namespace Infra.Persistance.Entities;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<ThinkingContent> ThinkingContents { get; set; } = [];
}

public class ThinkingContent
{
    public string BeforeThink { get; set; } = string.Empty;
    public string ThinkText { get; set; } = string.Empty;
    public string AfterThink { get; set; } = string.Empty;
}

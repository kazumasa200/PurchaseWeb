namespace Infra.Persistance.Entities;

public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public List<ThinkingContent> ThinkingContents { get; set; } = new();
}

public class ThinkingContent
{
    public string BeforeThink { get; set; }
    public string ThinkText { get; set; }
    public string AfterThink { get; set; }
}

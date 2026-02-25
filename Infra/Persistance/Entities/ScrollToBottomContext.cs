namespace Infra.Persistance.Entities;

public class ScrollToBottomContext
{
    public bool IsRequestScrollToBottom { get; private set; }

    public void RequestScrollToBottom()
    {
        IsRequestScrollToBottom = true;
    }

    public void Reset()
    {
        IsRequestScrollToBottom = false;
    }
}

namespace Infra.Repositories;

public class UserState(AppSettings appSettings)
{
    public AppSettings AppSettings { get; set; } = appSettings;

    private bool _isStoreUser;

    public event Action OnStateChanged;

    public bool IsStoreUser
    {
        get => _isStoreUser;
        set
        {
            _isStoreUser = value;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public void SetStoreUser(string password)
    {
        IsStoreUser = password == AppSettings.StorePassword;
    }
}
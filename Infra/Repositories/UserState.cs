namespace Infra.Repositories;

public class UserState(AppSettings appSettings)
{
    public AppSettings AppSettings { get; set; } = appSettings;

    public bool IsStoreUser { get; set; } = false;

    public void SetStoreUser(string password)
    {
        IsStoreUser = password == AppSettings.StorePassword;
    }
}
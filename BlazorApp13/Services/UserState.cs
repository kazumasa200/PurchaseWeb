using Infra.Repositories;
using Microsoft.JSInterop;

namespace PurchaseWeb.Services;

public class UserState
{
    private readonly AppSettings _appSettings;
    private readonly ITenantProvider _tenantProvider;
    private readonly IJSRuntime? _jsRuntime;
    private bool _initialized = false;

    public UserState(AppSettings appSettings, ITenantProvider tenantProvider, IJSRuntime jsRuntime)
    {
        _appSettings = appSettings;
        _tenantProvider = tenantProvider;
        _jsRuntime = jsRuntime;
        AppSettings = appSettings;
    }

    public AppSettings AppSettings { get; set; }

    private bool _isStoreUser;

    public string? CurrentTenantId { get; set; }
    public string? CurrentTenantName { get; set; }

    public event Action OnStateChanged = default!;

    public bool IsStoreUser
    {
        get => _isStoreUser;
        set
        {
            _isStoreUser = value;
            NotifyStateChanged();
        }
    }

    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    public async Task InitializeAsync()
    {
        if (_initialized || _jsRuntime == null) return;

        try
        {
            var isStoreUser = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "isStoreUser");
            var tenantId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentTenantId");
            var tenantName = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentTenantName");

            Console.WriteLine($"InitializeAsync - isStoreUser: {isStoreUser}, tenantId: {tenantId}, tenantName: {tenantName}");

            if (!string.IsNullOrEmpty(isStoreUser) && isStoreUser == "true")
            {
                _isStoreUser = true;
            }

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(tenantName))
            {
                CurrentTenantId = tenantId;
                CurrentTenantName = tenantName;
                _tenantProvider.SetTenantId(tenantId);
            }

            _initialized = true;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"InitializeAsync Error: {ex.Message}");
        }
    }

    public async Task SetStoreUserAsync(string password)
    {
        IsStoreUser = password == AppSettings.StorePassword;

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isStoreUser", IsStoreUser.ToString().ToLower());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetStoreUserAsync Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// テナントを設定する
    /// </summary>
    public async Task SetTenantAsync(string tenantId, string tenantName)
    {
        CurrentTenantId = tenantId;
        CurrentTenantName = tenantName;
        _tenantProvider.SetTenantId(tenantId);

        Console.WriteLine($"SetTenantAsync - tenantId: {tenantId}, tenantName: {tenantName}");

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentTenantId", tenantId);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentTenantName", tenantName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetTenantAsync Error: {ex.Message}");
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// ログアウト
    /// </summary>
    public async Task LogoutAsync()
    {
        _isStoreUser = false;
        CurrentTenantId = null;
        CurrentTenantName = null;

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "isStoreUser");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentTenantId");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentTenantName");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LogoutAsync Error: {ex.Message}");
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// テナントがセットされているか
    /// </summary>
    public bool HasTenant => !string.IsNullOrEmpty(CurrentTenantId);
}

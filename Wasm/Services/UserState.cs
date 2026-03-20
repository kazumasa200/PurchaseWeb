using Microsoft.JSInterop;

namespace PurchaseWeb.Wasm.Services;

public class UserState
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IJSRuntime? _jsRuntime;
    private bool _initialized = false;
    private bool _isStoreUser;

    public string? CurrentTenantId { get; set; }
    public string? CurrentTenantName { get; set; }
    public event Action OnStateChanged = default!;

    public UserState(ITenantProvider tenantProvider, IJSRuntime jsRuntime)
    {
        _tenantProvider = tenantProvider;
        _jsRuntime = jsRuntime;
    }

    public bool IsStoreUser
    {
        get => _isStoreUser;
        set { _isStoreUser = value; NotifyStateChanged(); }
    }

    public bool HasTenant => !string.IsNullOrEmpty(CurrentTenantId);
    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    public async Task InitializeAsync()
    {
        if (_initialized || _jsRuntime == null) return;
        try
        {
            var isStoreUser = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "isStoreUser");
            var tenantId    = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentTenantId");
            var tenantName  = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentTenantName");

            if (isStoreUser == "true") _isStoreUser = true;

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(tenantName))
            {
                CurrentTenantId   = tenantId;
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

    public async Task SetAuthenticatedAsync(bool isAuthenticated)
    {
        _isStoreUser = isAuthenticated;
        NotifyStateChanged();

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isStoreUser",
                    isAuthenticated.ToString().ToLower());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetAuthenticatedAsync localStorage Error: {ex.Message}");
            }
        }
    }

    public async Task SetTenantAsync(string tenantId, string tenantName)
    {
        CurrentTenantId   = tenantId;
        CurrentTenantName = tenantName;
        _tenantProvider.SetTenantId(tenantId);

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentTenantId",   tenantId);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentTenantName", tenantName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetTenantAsync Error: {ex.Message}");
            }
        }
        NotifyStateChanged();
    }

    public async Task LogoutAsync()
    {
        _isStoreUser      = false;
        CurrentTenantId   = null;
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
}

using Microsoft.JSInterop;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Services;

/// <summary>
/// 画面の出し分けに使う状態。
///
/// IsStoreUser は「表示を切り替えるためのヒント」でしかない。
/// ここを書き換えても API は HttpOnly Cookie しか見ないので、店員の操作は通らない。
/// 起動時の判定も localStorage ではなくサーバー（/api/auth/me）に聞く。
/// </summary>
public class UserState
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IJSRuntime? _jsRuntime;
    private readonly IAuthRepository _authRepo;
    private bool _initialized = false;
    private bool _isStoreUser;

    public string? CurrentTenantId { get; set; }
    public string? CurrentTenantName { get; set; }
    public event Action OnStateChanged = default!;

    public UserState(ITenantProvider tenantProvider, IJSRuntime jsRuntime, IAuthRepository authRepo)
    {
        _tenantProvider = tenantProvider;
        _jsRuntime = jsRuntime;
        _authRepo = authRepo;
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
            // 店員かどうかはサーバーの Cookie が決める。localStorage は見ない。
            _isStoreUser = await _authRepo.IsAuthenticatedAsync();

            var tenantId    = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentTenantId");
            var tenantName  = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentTenantName");

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

    /// <summary>
    /// ログイン成否を画面に反映する。Cookie はサーバーが発行済みなので、ここでは保存しない。
    /// </summary>
    public Task SetAuthenticatedAsync(bool isAuthenticated)
    {
        _isStoreUser = isAuthenticated;
        NotifyStateChanged();
        return Task.CompletedTask;
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

        // サーバー側の Cookie も確実に落とす（ここを忘れると認可が残ったままになる）
        try
        {
            await _authRepo.LogoutAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LogoutAsync API Error: {ex.Message}");
        }

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

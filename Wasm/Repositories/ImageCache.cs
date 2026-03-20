using System.Collections.Concurrent;

namespace PurchaseWeb.Wasm.Repositories;

/// <summary>ページ間で画像Base64を共有するインメモリキャッシュ</summary>
public static class ImageCache
{
    private static readonly ConcurrentDictionary<string, string?> _cache = new();

    public static void Set(string productId, string? base64)
        => _cache[productId] = base64;

    public static bool TryGet(string productId, out string? base64)
        => _cache.TryGetValue(productId, out base64);

    public static void Remove(string productId)
        => _cache.TryRemove(productId, out _);

    public static void Clear()
        => _cache.Clear();
}

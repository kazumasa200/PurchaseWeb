using System.Collections.Concurrent;

namespace PurchaseWeb.Wasm.Services;

public static class ImageCache
{
    private static readonly ConcurrentDictionary<string, string?> _cache = new();

    public static bool TryGet(string productId, out string? image)
        => _cache.TryGetValue(productId, out image);

    public static void Set(string productId, string? image)
        => _cache[productId] = image;

    public static void Remove(string productId)
        => _cache.TryRemove(productId, out _);

    public static void Clear() => _cache.Clear();
}

using System.Collections.Concurrent;

namespace PurchaseWeb.Client.Services;

/// <summary>
/// 商品画像のクライアントサイドメモリキャッシュ。
/// static なので WASM の生存期間中（タブを閉じるまで）保持される。
/// </summary>
public static class ImageCache
{
    private static readonly ConcurrentDictionary<string, string?> _cache = new();

    /// <summary>キャッシュから画像を取得。存在すれば true。</summary>
    public static bool TryGet(string productId, out string? image)
        => _cache.TryGetValue(productId, out image);

    /// <summary>画像をキャッシュに保存。</summary>
    public static void Set(string productId, string? image)
        => _cache[productId] = image;

    /// <summary>特定商品のキャッシュを削除（画像更新時に呼ぶ）。</summary>
    public static void Remove(string productId)
        => _cache.TryRemove(productId, out _);

    /// <summary>全キャッシュをクリア。</summary>
    public static void Clear() => _cache.Clear();
}

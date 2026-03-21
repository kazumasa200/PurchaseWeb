using MudBlazor;

namespace PurchaseWeb.Wasm.Helpers;

/// <summary>在庫数をUIに表示するための共通ヘルパー（顧客向け表示用）</summary>
public static class StockDisplay
{
    /// <summary>在庫数に応じた表示テキスト</summary>
    public static string GetText(int? stock) => stock switch
    {
        null => "在庫：無制限",
        0    => "在庫なし",
        <= 5 => $"残り {stock} 個",
        _    => $"在庫：{stock} 個"
    };

    /// <summary>在庫数に応じた MudBlazor 表示色</summary>
    public static Color GetColor(int? stock) => stock switch
    {
        null => Color.Default,
        0    => Color.Error,
        <= 5 => Color.Warning,
        _    => Color.Success
    };
}

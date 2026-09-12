using SkiaSharp;

namespace PurchaseWeb.Api.Services;

/// <summary>
/// アップロードされた画像を縮小して WebP にする。
///
/// 商品サムネの表示は 120px なのに、スマホで撮った 3〜6MB の写真がそのまま
/// base64 で DB に入っていた。12枚で 36.7MB あり、イベント時に
/// 客の端末とサーバーの両方を詰まらせていた。
///
/// 方針:
///   - 長辺を <see cref="MaxDimension"/> に収める（拡大はしない）
///   - WebP で再エンコードする
///   - EXIF の回転指示を焼き込む（WebP には EXIF を持ち越さないため、
///     ここで適用しないと写真が横倒しになる）
///   - 何かあったら元の base64 をそのまま返す。**利用者の画像を失わないことを優先する**
/// </summary>
public static class ImageOptimizer
{
    /// <summary>長辺の上限。表示は120pxなので、拡大表示や高DPIを見ても800あれば足りる。</summary>
    public const int MaxDimension = 800;

    /// <summary>WebP の品質。80 は写真で劣化がほぼ見えない実用域。</summary>
    public const int Quality = 80;

    public static string Optimize(string base64)
    {
        byte[] source;
        try { source = Convert.FromBase64String(base64); }
        catch (FormatException) { return base64; }

        var optimized = Optimize(source);
        return optimized is null ? base64 : Convert.ToBase64String(optimized);
    }

    /// <summary>変換できなければ null を返す（呼び出し側は元データを維持する）</summary>
    public static byte[]? Optimize(byte[] source)
    {
        try
        {
            using var data = SKData.CreateCopy(source);
            using var codec = SKCodec.Create(data);
            if (codec is null) return null;

            using var bitmap = SKBitmap.Decode(codec);
            if (bitmap is null) return null;

            using var oriented = ApplyOrigin(bitmap, codec.EncodedOrigin);
            using var scaled = Scale(oriented);
            using var image = SKImage.FromBitmap(scaled);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, Quality);
            if (encoded is null) return null;

            var result = encoded.ToArray();

            // すでに小さい画像を WebP にして逆に太る場合は元のままにする
            return result.Length < source.Length ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static SKBitmap Scale(SKBitmap src)
    {
        var longSide = Math.Max(src.Width, src.Height);
        if (longSide <= MaxDimension) return src.Copy();

        var ratio = (double)MaxDimension / longSide;
        var info = new SKImageInfo(
            Math.Max(1, (int)Math.Round(src.Width * ratio)),
            Math.Max(1, (int)Math.Round(src.Height * ratio)));

        return src.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell)) ?? src.Copy();
    }

    /// <summary>EXIF の向きをピクセルに反映する。</summary>
    private static SKBitmap ApplyOrigin(SKBitmap src, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.Default or SKEncodedOrigin.TopLeft)
            return src.Copy();

        // 90/270度回転では縦横が入れ替わる
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var w = swap ? src.Height : src.Width;
        var h = swap ? src.Width : src.Height;

        // Skia 本家の SkEncodedOriginToMatrix と同じ変換。
        // 値は (scaleX, skewX, transX, skewY, scaleY, transY) で、w/h は変換【後】の寸法。
        // 自前で Rotate/Scale/Translate を並べると順序を間違えやすいので行列を直接置く。
        var m = origin switch
        {
            SKEncodedOrigin.TopRight    => new SKMatrix(-1,  0, w,  0,  1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1,  0, w,  0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft  => new SKMatrix( 1,  0, 0,  0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop     => new SKMatrix( 0,  1, 0,  1,  0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop    => new SKMatrix( 0, -1, w,  1,  0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix( 0, -1, w, -1,  0, h, 0, 0, 1),
            SKEncodedOrigin.LeftBottom  => new SKMatrix( 0,  1, 0, -1,  0, h, 0, 0, 1),
            _                           => SKMatrix.Identity,
        };

        var rotated = new SKBitmap(w, h);
        using var canvas = new SKCanvas(rotated);
        canvas.SetMatrix(m);
        canvas.DrawBitmap(src, 0, 0);
        canvas.Flush();
        return rotated;
    }
}

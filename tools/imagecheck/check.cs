#:package SkiaSharp@3.119.0

// Api/Services/ImageOptimizer.cs の自己チェック。ロジックを変えたらここも動かすこと。
//
//   1. 実データを用意する（本番の product_images から取得し、src/<image_id>.b64 として保存）
//   2. dotnet run check.cs
//
// 長辺が上限以内か・縦横比が保たれているか・WebP になっているかを検査する。
// EXIF の向きは Pillow の ImageOps.exif_transpose と寸法を突き合わせて確認した
// （2026-09-12、本番の12枚すべて一致。うち1枚が RightTop）。
using SkiaSharp;

const int MaxDimension = 800;
const int Quality = 80;

var files = Directory.GetFiles("src", "*.b64").OrderBy(f => f).ToList();
if (files.Count == 0) { Console.WriteLine("src/*.b64 が無い"); return; }

long before = 0, after = 0;
var failures = new List<string>();

foreach (var f in files)
{
    var source = Convert.FromBase64String(File.ReadAllText(f).Trim());
    before += source.Length;

    var result = Optimize(source);
    var name = Path.GetFileNameWithoutExtension(f)[..8];

    if (result is null)
    {
        Console.WriteLine($"  {name}  変換せず（元のまま）");
        after += source.Length;
        failures.Add($"{name}: 変換できなかった");
        continue;
    }

    using var outBmp = SKBitmap.Decode(result);
    using var srcCodec = SKCodec.Create(SKData.CreateCopy(source));
    var so = srcCodec!.Info;
    var origin = srcCodec.EncodedOrigin;
    var swapped = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
        or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
    var orientedW = swapped ? so.Height : so.Width;
    var orientedH = swapped ? so.Width : so.Height;

    after += result.Length;
    Console.WriteLine($"  {name}  {so.Width}x{so.Height} [{origin}] → {outBmp.Width}x{outBmp.Height}   "
                      + $"{source.Length / 1024.0:F0} KB → {result.Length / 1024.0:F0} KB");

    // 長辺が上限を超えていないこと
    if (Math.Max(outBmp.Width, outBmp.Height) > MaxDimension)
        failures.Add($"{name}: 長辺が {MaxDimension} を超えている");

    // 縦横比が保たれていること（回転ぶんを考慮した比率と比べる）
    var expected = (double)orientedW / orientedH;
    var actual = (double)outBmp.Width / outBmp.Height;
    if (Math.Abs(expected - actual) > 0.02)
        failures.Add($"{name}: 縦横比がずれている 期待 {expected:F3} / 実際 {actual:F3}");

    // WebP になっていること
    if (!(result.Length > 12 && result[0] == 0x52 && result[8] == 0x57 && result[9] == 0x45))
        failures.Add($"{name}: WebP になっていない");

    File.WriteAllBytes($"out_{name}.webp", result);
}

Console.WriteLine();
Console.WriteLine($"合計: {before / 1048576.0:F2} MB → {after / 1048576.0:F2} MB  （{(double)after / before:P1}）");

if (failures.Count > 0)
{
    Console.WriteLine("\n--- 問題 ---");
    failures.ForEach(x => Console.WriteLine("  " + x));
    Environment.ExitCode = 1;
}
else Console.WriteLine("\nすべて期待どおり");

// --- 以下は Api/Services/ImageOptimizer.cs と同じロジック ---
static byte[]? Optimize(byte[] source)
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
        return result.Length < source.Length ? result : null;
    }
    catch { return null; }
}

static SKBitmap Scale(SKBitmap src)
{
    var longSide = Math.Max(src.Width, src.Height);
    if (longSide <= MaxDimension) return src.Copy();
    var ratio = (double)MaxDimension / longSide;
    var info = new SKImageInfo(
        Math.Max(1, (int)Math.Round(src.Width * ratio)),
        Math.Max(1, (int)Math.Round(src.Height * ratio)));
    return src.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell)) ?? src.Copy();
}

static SKBitmap ApplyOrigin(SKBitmap src, SKEncodedOrigin origin)
{
    if (origin is SKEncodedOrigin.Default or SKEncodedOrigin.TopLeft) return src.Copy();
    var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
        or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
    var w = swap ? src.Height : src.Width;
    var h = swap ? src.Width : src.Height;
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

using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace iFramework;

/// <summary>
/// 针对游戏 HUD 数字的小型模板 OCR。模板目录放置 0.png..9.png 后即可工作，
/// 不依赖系统语言包或外部 OCR 服务。
/// </summary>
public sealed class TemplateDigitOcrEngine : IOcrEngine, IDisposable
{
    private const int NormalizedWidth = 24;
    private const int NormalizedHeight = 36;
    private readonly Dictionary<int, Mat> _templates = new();
    private readonly double _minimumScore;

    public TemplateDigitOcrEngine(string templateDirectory, double minimumScore = 0.6d)
    {
        if (minimumScore is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(nameof(minimumScore));
        _minimumScore = minimumScore;
        for (var digit = 0; digit <= 9; digit++)
        {
            var path = Path.Combine(templateDirectory, digit + ".png");
            if (!File.Exists(path)) continue;
            using var source = Cv2.ImRead(path, ImreadModes.Grayscale);
            if (!source.Empty()) _templates[digit] = NormalizeGlyph(source);
        }
    }

    public string Recognize(Bitmap image)
        => RecognizeNumber(image)?.ToString() ?? string.Empty;

    public IReadOnlyList<OcrTextResult> RecognizeAll(Bitmap image)
    {
        var text = Recognize(image);
        return string.IsNullOrEmpty(text)
            ? Array.Empty<OcrTextResult>()
            : new[] { new OcrTextResult(text, new Rect(0, 0, image.Width, image.Height)) };
    }

    public int? RecognizeNumber(Bitmap image)
    {
        if (_templates.Count < 10 || image.Width <= 0 || image.Height <= 0) return null;

        using var source = image.ToMat();
        using var gray = new Mat();
        switch (source.Channels())
        {
            case 1:
                source.CopyTo(gray);
                break;
            case 4:
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);
                break;
            default:
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                break;
        }

        using var binary = Binarize(gray);
        using var contourSource = binary.Clone();
        Cv2.FindContours(contourSource, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        var boxes = contours
            .Select(Cv2.BoundingRect)
            .Where(rect => rect.Width >= 1 && rect.Height >= Math.Max(2, binary.Rows / 4))
            .OrderBy(rect => rect.X)
            .ToArray();
        if (boxes.Length == 0) return null;

        var text = new System.Text.StringBuilder(boxes.Length);
        foreach (var box in boxes)
        {
            using var glyph = new Mat(binary, box);
            using var normalized = NormalizeBinaryGlyph(glyph);
            var bestDigit = -1;
            var bestScore = double.MinValue;

            foreach (var pair in _templates)
            {
                var difference = Cv2.Norm(normalized, pair.Value, NormTypes.L1)
                    / (255d * NormalizedWidth * NormalizedHeight);
                var score = 1d - difference;
                if (score <= bestScore) continue;
                bestScore = score;
                bestDigit = pair.Key;
            }

            if (bestDigit < 0 || bestScore < _minimumScore) return null;
            text.Append(bestDigit);
        }

        return int.TryParse(text.ToString(), out var number) ? number : null;
    }

    private static Mat NormalizeGlyph(Mat source)
    {
        using var binary = Binarize(source);
        return NormalizeBinaryGlyph(binary);
    }

    private static Mat NormalizeBinaryGlyph(Mat binary)
    {
        using var contourSource = binary.Clone();
        Cv2.FindContours(contourSource, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        if (contours.Length == 0) return new Mat(NormalizedHeight, NormalizedWidth, MatType.CV_8UC1, Scalar.Black);

        var bounds = contours.Select(Cv2.BoundingRect).Aggregate(Union);
        using var crop = new Mat(binary, bounds);
        var scale = Math.Min((NormalizedWidth - 4d) / crop.Width, (NormalizedHeight - 4d) / crop.Height);
        var width = Math.Max(1, (int)Math.Round(crop.Width * scale));
        var height = Math.Max(1, (int)Math.Round(crop.Height * scale));
        using var resized = new Mat();
        Cv2.Resize(crop, resized, new OpenCvSharp.Size(width, height), interpolation: InterpolationFlags.Nearest);

        var canvas = new Mat(NormalizedHeight, NormalizedWidth, MatType.CV_8UC1, Scalar.Black);
        using var target = new Mat(canvas, new OpenCvSharp.Rect(
            (NormalizedWidth - width) / 2,
            (NormalizedHeight - height) / 2,
            width,
            height));
        resized.CopyTo(target);
        return canvas;
    }

    private static Mat Binarize(Mat source)
    {
        var gray = source;
        Mat? converted = null;
        if (source.Channels() != 1)
        {
            converted = new Mat();
            Cv2.CvtColor(
                source,
                converted,
                source.Channels() == 4 ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY);
            gray = converted;
        }

        var binary = new Mat();
        Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        converted?.Dispose();

        // HUD 通常是亮字暗底；若阈值后白色占多数，则反转为白字黑底。
        if (Cv2.CountNonZero(binary) > binary.Rows * binary.Cols / 2)
            Cv2.BitwiseNot(binary, binary);
        return binary;
    }

    private static OpenCvSharp.Rect Union(OpenCvSharp.Rect left, OpenCvSharp.Rect right)
    {
        var x = Math.Min(left.X, right.X);
        var y = Math.Min(left.Y, right.Y);
        var rightEdge = Math.Max(left.Right, right.Right);
        var bottomEdge = Math.Max(left.Bottom, right.Bottom);
        return new OpenCvSharp.Rect(x, y, rightEdge - x, bottomEdge - y);
    }

    public void Dispose()
    {
        foreach (var template in _templates.Values) template.Dispose();
        _templates.Clear();
    }
}

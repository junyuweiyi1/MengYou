using PaddleOCRSharp;

namespace iFramework;

/// <summary>基于 PaddleOCR 的离线中文 OCR 引擎。</summary>
public sealed class PaddleOcrEngine : IOcrEngine, IDisposable
{
    private readonly PaddleOCREngine _engine = new();

    public string Recognize(Bitmap image)
        => string.Join(Environment.NewLine, RecognizeAll(image).Select(result => result.Text));

    public int? RecognizeNumber(Bitmap image)
        => int.TryParse(Recognize(image), out var number) ? number : null;

    public IReadOnlyList<OcrTextResult> RecognizeAll(Bitmap image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var result = _engine.DetectText(image);
        return result.TextBlocks
            .Where(block => !string.IsNullOrWhiteSpace(block.Text))
            .Select(block =>
            {
                var left = block.BoxPoints.Min(point => point.X);
                var top = block.BoxPoints.Min(point => point.Y);
                var right = block.BoxPoints.Max(point => point.X);
                var bottom = block.BoxPoints.Max(point => point.Y);
                return new OcrTextResult(block.Text, new Rect(left, top, right - left, bottom - top));
            })
            .ToArray();
    }

    public void Dispose() => _engine.Dispose();
}

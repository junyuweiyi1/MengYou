namespace iFramework;

/// <summary>
/// OCR 引擎抽象：允许后续切换 Tesseract / PaddleOCR 等实现。
/// </summary>
public interface IOcrEngine
{
    /// <summary>当前 OCR 实现是否具备实际识别能力。</summary>
    bool IsAvailable { get; }

    /// <summary>识别整张位图内的文字。</summary>
    string Recognize(Bitmap image);

    /// <summary>识别数字（内部做二值化 + 白名单）。</summary>
    int? RecognizeNumber(Bitmap image);
}

/// <summary>
/// 占位 OCR 实现：返回空串。
/// 后续替换为 PaddleOCR.Onnx 或 Tesseract 的真实实现。
/// </summary>
public sealed class NoopOcrEngine : IOcrEngine
{
    public bool IsAvailable => false;

    /// <inheritdoc/>
    public string Recognize(Bitmap image) => string.Empty;

    /// <inheritdoc/>
    public int? RecognizeNumber(Bitmap image) => null;
}

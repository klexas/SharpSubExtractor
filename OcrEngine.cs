using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SubtitleExtractor.Models;
using Tesseract;

namespace SubtitleExtractor;

public sealed class OcrEngine : IDisposable
{
    private readonly TesseractEngine _engine;
    private readonly int _confidenceThreshold;

    public OcrEngine(string language, int confidenceThreshold, string? tessDataPath = null)
    {
        _confidenceThreshold = confidenceThreshold;
        string dataPath = tessDataPath ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
        _engine = new TesseractEngine(dataPath, language, EngineMode.Default);
    }

    public OcrResult Recognise(Image<Rgba32> image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, PngFormat.Instance);
        ms.Position = 0;

        using var pix = Pix.LoadFromMemory(ms.ToArray());
        using var page = _engine.Process(pix);

        string text = page.GetText().Trim();
        float confidence = page.GetMeanConfidence() * 100f;

        if (confidence < _confidenceThreshold)
            return new OcrResult(string.Empty, confidence);

        return new OcrResult(NormaliseText(text), confidence);
    }

    private static string NormaliseText(string raw) =>
        System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");

    public void Dispose() => _engine.Dispose();
}

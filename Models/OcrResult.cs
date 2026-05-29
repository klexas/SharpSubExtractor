namespace SubtitleExtractor.Models;

public record OcrResult(string Text, float Confidence)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
}

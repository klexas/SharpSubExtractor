namespace SubtitleExtractor.Writers;

public static class SubtitleWriterFactory
{
    public static ISubtitleWriter Create(string format) => format.ToLowerInvariant().TrimStart('.') switch
    {
        "srt" => new SrtWriter(),
        "vtt" => new VttWriter(),
        "ass" => new AssWriter(),
        _ => throw new ArgumentException($"Unsupported subtitle format: '{format}'. Supported: srt, vtt, ass")
    };
}

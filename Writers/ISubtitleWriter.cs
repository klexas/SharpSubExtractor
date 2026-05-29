using SubtitleExtractor.Models;

namespace SubtitleExtractor.Writers;

public interface ISubtitleWriter
{
    string Extension { get; }
    Task WriteAsync(IEnumerable<SubtitleEntry> entries, Stream output);
}

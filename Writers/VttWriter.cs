using System.Text;
using SubtitleExtractor.Models;

namespace SubtitleExtractor.Writers;

public sealed class VttWriter : ISubtitleWriter
{
    public string Extension => ".vtt";

    public async Task WriteAsync(IEnumerable<SubtitleEntry> entries, Stream output)
    {
        using var writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync("WEBVTT");
        await writer.WriteLineAsync();

        foreach (var entry in entries)
        {
            await writer.WriteLineAsync(entry.Index.ToString());
            await writer.WriteLineAsync($"{FormatTimestamp(entry.Start)} --> {FormatTimestamp(entry.End)}");
            await writer.WriteLineAsync(entry.Text);
            await writer.WriteLineAsync();
        }
    }

    private static string FormatTimestamp(TimeSpan t) =>
        $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}";
}

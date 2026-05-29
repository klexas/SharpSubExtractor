using System.Text;
using SubtitleExtractor.Models;

namespace SubtitleExtractor.Writers;

public sealed class SrtWriter : ISubtitleWriter
{
    public string Extension => ".srt";

    public async Task WriteAsync(IEnumerable<SubtitleEntry> entries, Stream output)
    {
        using var writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: true);

        foreach (var entry in entries)
        {
            await writer.WriteLineAsync(entry.Index.ToString());
            await writer.WriteLineAsync($"{FormatTimestamp(entry.Start)} --> {FormatTimestamp(entry.End)}");
            await writer.WriteLineAsync(entry.Text);
            await writer.WriteLineAsync();
        }
    }

    private static string FormatTimestamp(TimeSpan t) =>
        $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2},{t.Milliseconds:D3}";
}

using System.Text;
using SubtitleExtractor.Models;

namespace SubtitleExtractor.Writers;

public sealed class AssWriter : ISubtitleWriter
{
    public string Extension => ".ass";

    public async Task WriteAsync(IEnumerable<SubtitleEntry> entries, Stream output)
    {
        using var writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync("[Script Info]");
        await writer.WriteLineAsync("ScriptType: v4.00+");
        await writer.WriteLineAsync("Collisions: Normal");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("[V4+ Styles]");
        await writer.WriteLineAsync("Format: Name, Fontname, Fontsize, PrimaryColour, Bold, Italic, Underline, Alignment, BorderStyle, Outline, Shadow, MarginL, MarginR, MarginV, Encoding");
        await writer.WriteLineAsync("Style: Default,Arial,20,&H00FFFFFF,0,0,0,2,1,2,0,10,10,10,1");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("[Events]");
        await writer.WriteLineAsync("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var entry in entries)
        {
            await writer.WriteLineAsync(
                $"Dialogue: 0,{FormatTimestamp(entry.Start)},{FormatTimestamp(entry.End)},Default,,0,0,0,,{entry.Text}");
        }
    }

    private static string FormatTimestamp(TimeSpan t) =>
        $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds / 10:D2}";
}

namespace SubtitleExtractor.Models;

public record SubtitleEntry(int Index, TimeSpan Start, TimeSpan End, string Text);

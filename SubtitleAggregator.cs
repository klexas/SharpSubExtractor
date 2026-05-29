using SubtitleExtractor.Models;

namespace SubtitleExtractor;

public sealed class SubtitleAggregator
{
    private readonly TimeSpan _minDuration;
    private readonly TimeSpan _mergeTolerance;
    private readonly double _similarityThreshold;

    private string? _currentText;
    private TimeSpan _currentStart;
    private TimeSpan _currentEnd;
    private readonly List<SubtitleEntry> _entries = [];
    private int _index = 1;

    public SubtitleAggregator(
        TimeSpan? minDuration = null,
        TimeSpan? mergeTolerance = null,
        double similarityThreshold = 0.60)
    {
        _minDuration = minDuration ?? TimeSpan.FromSeconds(1.5);
        _mergeTolerance = mergeTolerance ?? TimeSpan.FromSeconds(1.5);
        _similarityThreshold = similarityThreshold;
    }

    public void Feed(TimeSpan timestamp, OcrResult result)
    {
        string text = result.IsEmpty ? string.Empty : result.Text;

        if (_currentText is null)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (!TryReopenLastEntry(timestamp, text))
                    BeginEntry(timestamp, text);
            }
            return;
        }

        if (!string.IsNullOrEmpty(text) && Similarity(text, _currentText) >= _similarityThreshold)
        {
            _currentEnd = timestamp;
            return;
        }

        CommitEntry();

        if (!string.IsNullOrEmpty(text))
        {
            if (!TryReopenLastEntry(timestamp, text))
                BeginEntry(timestamp, text);
        }
    }

    public IReadOnlyList<SubtitleEntry> Flush()
    {
        if (_currentText is not null)
            CommitEntry();
        return _entries.AsReadOnly();
    }

    // If a new text is similar to the last committed entry and close enough in time,
    // remove that entry and reopen it as the current entry, bridging the gap caused
    // by empty OCR frames in between.
    private bool TryReopenLastEntry(TimeSpan timestamp, string text)
    {
        if (_entries.Count == 0) return false;
        var last = _entries[_entries.Count - 1];
        if (timestamp - last.End > _mergeTolerance) return false;
        if (Similarity(text, last.Text) < _similarityThreshold) return false;

        _entries.RemoveAt(_entries.Count - 1);
        _index--;
        _currentText = last.Text;
        _currentStart = last.Start;
        _currentEnd = timestamp;
        return true;
    }

    private void BeginEntry(TimeSpan start, string text)
    {
        _currentText = text;
        _currentStart = start;
        _currentEnd = start;
    }

    private void CommitEntry()
    {
        if (_currentText is null) return;

        var duration = _currentEnd - _currentStart;
        var end = duration < _minDuration ? _currentStart + _minDuration : _currentEnd;

        _entries.Add(new SubtitleEntry(_index++, _currentStart, end, _currentText));
        _currentText = null;
    }

    private static double Similarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return 1.0;

        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        int maxLen = Math.Max(a.Length, b.Length);
        return 1.0 - (double)LevenshteinDistance(a, b) / maxLen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int m = a.Length, n = b.Length;
        var prev = new int[n + 1];
        var curr = new int[n + 1];

        for (int j = 0; j <= n; j++) prev[j] = j;

        for (int i = 1; i <= m; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= n; j++)
                curr[j] = a[i - 1] == b[j - 1]
                    ? prev[j - 1]
                    : 1 + Math.Min(prev[j - 1], Math.Min(prev[j], curr[j - 1]));
            (prev, curr) = (curr, prev);
        }

        return prev[n];
    }
}

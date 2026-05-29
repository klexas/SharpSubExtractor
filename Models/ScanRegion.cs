namespace SubtitleExtractor.Models;

public record ScanRegion(double Top, double Bottom, double Left, double Right)
{
    public static ScanRegion LowerQuarter => new(0.75, 1.0, 0.0, 1.0);

    public (int X, int Y, int Width, int Height) ToPixelRect(int frameWidth, int frameHeight)
    {
        int x = (int)(Left * frameWidth);
        int y = (int)(Top * frameHeight);
        int w = (int)((Right - Left) * frameWidth);
        int h = (int)((Bottom - Top) * frameHeight);
        return (x, y, w, h);
    }
}

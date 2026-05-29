using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SubtitleExtractor.Models;

namespace SubtitleExtractor;

public sealed class RegionCropper
{
    public Image<Rgba32> Crop(Image<Rgba32> frame, ScanRegion region, bool preprocess = true)
    {
        var (x, y, w, h) = region.ToPixelRect(frame.Width, frame.Height);

        var cropped = frame.Clone(ctx =>
        {
            ctx.Crop(new Rectangle(x, y, w, h));
            if (preprocess)
            {
                ctx.Grayscale();
                ctx.Contrast(1.5f);
            }
        });

        return cropped;
    }
}

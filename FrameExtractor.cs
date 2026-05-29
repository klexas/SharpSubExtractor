using FFMpegCore;
using FFMpegCore.Pipes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SubtitleExtractor;

public sealed class FrameExtractor
{
    public async IAsyncEnumerable<(TimeSpan Timestamp, Image<Rgba32> Frame)> ExtractAsync(
        string videoPath,
        double fps)
    {
        var options = new FFOptions() { BinaryFolder = "c:\\Tools\\" };

        var mediaInfo = await FFProbe.AnalyseAsync(videoPath, options);
        double duration = mediaInfo.Duration.TotalSeconds;
        double interval = 1.0 / fps;

        for (double t = 0; t < duration; t += interval)
        {
            var timestamp = TimeSpan.FromSeconds(t);

            using var ms = new MemoryStream();
            var sink = new StreamPipeSink(ms);

            await FFMpegArguments
                .FromFileInput(videoPath, false, o => o.Seek(timestamp))
                .OutputToPipe(sink, o => o
                    .WithVideoCodec("png")
                    .ForceFormat("image2pipe")
                    .WithFrameOutputCount(1))
                .ProcessAsynchronously(true, options);

            ms.Position = 0;
            if (ms.Length == 0) continue;

            var image = Image.Load<Rgba32>(ms);
            yield return (timestamp, image);
        }
    }
}

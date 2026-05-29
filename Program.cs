using System.CommandLine;
using SubtitleExtractor;
using SubtitleExtractor.Models;
using SubtitleExtractor.Writers;

var inputOption = new Option<FileInfo>(
    ["--input", "-i"],
    "Path to the input video file.")
{ IsRequired = true };

var outputOption = new Option<string>(
    ["--output", "-o"],
    "Path to the output subtitle file.")
{ IsRequired = true };

var formatOption = new Option<string>(
    ["--format", "-f"],
    () => "srt",
    "Output subtitle format: srt, vtt, ass.");

var regionTopOption = new Option<double>(
    "--region-top",
    () => 0.75,
    "Top edge of scan region as a fraction of frame height (0.0–1.0).");

var regionBottomOption = new Option<double>(
    "--region-bottom",
    () => 1.0,
    "Bottom edge of scan region as a fraction of frame height (0.0–1.0).");

var regionLeftOption = new Option<double>(
    "--region-left",
    () => 0.0,
    "Left edge of scan region as a fraction of frame width (0.0–1.0).");

var regionRightOption = new Option<double>(
    "--region-right",
    () => 1.0,
    "Right edge of scan region as a fraction of frame width (0.0–1.0).");

var fpsOption = new Option<double>(
    "--fps",
    () => 2.0,
    "Number of frames per second to sample from the video.");

var languageOption = new Option<string>(
    "--language",
    () => "eng",
    "Tesseract language pack to use (e.g. eng, fra, deu).");

var thresholdOption = new Option<int>(
    "--threshold",
    () => 75,
    "OCR confidence threshold (0–100). Results below this are discarded.");

var tessDataOption = new Option<string?>(
    "--tessdata",
    () => null,
    "Path to the tessdata directory. Defaults to 'tessdata' next to the executable.");

var preprocessOption = new Option<bool>(
    "--preprocess",
    () => true,
    "Apply grayscale + contrast pre-processing to cropped frames before OCR.");

var rootCommand = new RootCommand("Extract hardcoded subtitles from a video file using OCR.")
{
    inputOption, outputOption, formatOption,
    regionTopOption, regionBottomOption, regionLeftOption, regionRightOption,
    fpsOption, languageOption, thresholdOption, tessDataOption, preprocessOption
};

rootCommand.SetHandler(async ctx =>
{
    var input      = ctx.ParseResult.GetValueForOption(inputOption)!;
    var output     = ctx.ParseResult.GetValueForOption(outputOption)!;
    var format     = ctx.ParseResult.GetValueForOption(formatOption)!;
    var regionTop  = ctx.ParseResult.GetValueForOption(regionTopOption);
    var regionBot  = ctx.ParseResult.GetValueForOption(regionBottomOption);
    var regionLeft = ctx.ParseResult.GetValueForOption(regionLeftOption);
    var regionRight= ctx.ParseResult.GetValueForOption(regionRightOption);
    var fps        = ctx.ParseResult.GetValueForOption(fpsOption);
    var language   = ctx.ParseResult.GetValueForOption(languageOption)!;
    var threshold  = ctx.ParseResult.GetValueForOption(thresholdOption);
    var tessData   = ctx.ParseResult.GetValueForOption(tessDataOption);
    var preprocess = ctx.ParseResult.GetValueForOption(preprocessOption);

    if (!input.Exists)
    {
        Console.Error.WriteLine($"Error: input file not found: {input.FullName}");
        ctx.ExitCode = 1;
        return;
    }

    ValidateFraction(regionTop,  "--region-top");
    ValidateFraction(regionBot,  "--region-bottom");
    ValidateFraction(regionLeft, "--region-left");
    ValidateFraction(regionRight,"--region-right");

    if (regionTop >= regionBot)
    {
        Console.Error.WriteLine("Error: --region-top must be less than --region-bottom.");
        ctx.ExitCode = 1;
        return;
    }

    var region = new ScanRegion(regionTop, regionBot, regionLeft, regionRight);
    var writer = SubtitleWriterFactory.Create(format);

    // Ensure output path has the correct extension.
    string outputPath = output;
    if (!outputPath.EndsWith(writer.Extension, StringComparison.OrdinalIgnoreCase))
        outputPath = Path.ChangeExtension(outputPath, writer.Extension);

    Console.WriteLine($"Input  : {input.FullName}");
    Console.WriteLine($"Output : {outputPath}");
    Console.WriteLine($"Format : {format.ToUpperInvariant()}");
    Console.WriteLine($"Region : top={regionTop:P0} bottom={regionBot:P0} left={regionLeft:P0} right={regionRight:P0}");
    Console.WriteLine($"FPS    : {fps}");
    Console.WriteLine($"Lang   : {language}  Threshold: {threshold}");
    Console.WriteLine();

    using var ocr = new OcrEngine(language, threshold, tessData);
    var cropper = new RegionCropper();
    var aggregator = new SubtitleAggregator();
    var extractor = new FrameExtractor();

    int frameCount = 0;
    await foreach (var (timestamp, frame) in extractor.ExtractAsync(input.FullName, fps))
    {
        using var cropped = cropper.Crop(frame, region, preprocess);
        var result = ocr.Recognise(cropped);
        aggregator.Feed(timestamp, result);
        frame.Dispose();

        frameCount++;
        if (frameCount % 10 == 0)
            Console.Write($"\rProcessed {frameCount} frames — {timestamp:hh\\:mm\\:ss}   ");
    }

    Console.WriteLine();

    var entries = aggregator.Flush();
    Console.WriteLine($"Found {entries.Count} subtitle entries.");

    await using var outputStream = File.Create(outputPath);
    await writer.WriteAsync(entries, outputStream);

    Console.WriteLine($"Written to: {outputPath}");
});

void ValidateFraction(double value, string name)
{
    if (value is < 0.0 or > 1.0)
    {
        Console.Error.WriteLine($"Error: {name} must be between 0.0 and 1.0 (got {value}).");
        Environment.Exit(1);
    }
}

return await rootCommand.InvokeAsync(args);

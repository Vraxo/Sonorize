using SkiaSharp;

namespace Sonorize.Core.Core;

public class ImageAnalysisService
{
    public async Task<bool> IsAlbumArtBrightAsync(string audioFilePath)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath) || !File.Exists(audioFilePath))
        {
            return false;
        }

        return await Task.Run(() => AnalyzeAlbumArt(audioFilePath));
    }

    private static bool AnalyzeAlbumArt(string audioFilePath)
    {
        try
        {
            using TagLib.File file = TagLib.File.Create(audioFilePath);
            TagLib.IPicture? picture = file.Tag.Pictures.FirstOrDefault();

            if (picture is null || picture.Data.Data.Length == 0)
            {
                return false;
            }

            using SKBitmap? image = SKBitmap.Decode(picture.Data.Data);

            if (image is null)
            {
                return false;
            }

            using SKBitmap? thumbnail = image.Resize(
                new SKImageInfo(1, 1),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

            if (thumbnail is null)
            {
                return false;
            }

            SKColor pixel = thumbnail.GetPixel(0, 0);

            double luminance = (0.299 * pixel.Red)
                + (0.587 * pixel.Green)
                + (0.114 * pixel.Blue);

            return luminance > 140;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageAnalysis] Failed to analyze art: {ex.Message}");
            return false;
        }
    }
}

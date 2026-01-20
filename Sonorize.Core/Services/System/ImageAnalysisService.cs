using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sonorize.Core.Services.System;

public class ImageAnalysisService
{
    public async Task<bool> IsAlbumArtBrightAsync(string audioFilePath)
    {
        return !string.IsNullOrWhiteSpace(audioFilePath) && File.Exists(audioFilePath) && await Task.Run(() =>
        {
            try
            {
                // 1. Extract image data using TagLib (same logic as SchemeHandler)
                using var file = TagLib.File.Create(audioFilePath);
                var pic = file.Tag.Pictures.FirstOrDefault();

                if (pic is null || pic.Data.Data.Length == 0)
                {
                    return false;
                }

                // 2. Load into ImageSharp
                // ImageSharp is fully managed and cross-platform (no System.Drawing/GDI+)
                // Fix: Resolve ambiguity with System.Net.Mime.MediaTypeNames.Image
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(pic.Data.Data);

                // 3. Downsample to 1x1 pixel to get the average color efficiently
                image.Mutate(x => x.Resize(1, 1));

                // 4. Analyze Luminance
                Rgba32 pixel = image[0, 0];

                // Standard relative luminance formula: 0.299R + 0.587G + 0.114B
                double luminance = (0.299 * pixel.R) + (0.587 * pixel.G) + (0.114 * pixel.B);

                // Threshold: > 140 is generally considered "light"
                return luminance > 140;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageAnalysis] Failed to analyze art: {ex.Message}");
                return false;
            }
        });
    }
}
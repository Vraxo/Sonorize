using SkiaSharp;
using TuiColor = Terminal.Gui.Drawing.Color;

namespace Sonorize.Tui;

/// <summary>
/// Reads the embedded cover from an audio file and converts it to the pixel
/// buffer Terminal.Gui's ImageView draws.
/// </summary>
public static class AlbumArtLoader
{
    // The view scales the buffer to its own size; this only bounds decode cost
    // and what gets sent to terminals with a pixel graphics protocol.
    private const int MaxEdge = 256;

    /// <returns>Pixels indexed [x, y], or null when the file has no usable cover.</returns>
    public static TuiColor[,]? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using TagLib.File file = TagLib.File.Create(filePath);
            TagLib.IPicture? picture = file.Tag.Pictures.FirstOrDefault();

            if (picture is null || picture.Data.Data.Length == 0)
            {
                return null;
            }

            using SKBitmap? decodedImage = SKBitmap.Decode(picture.Data.Data);

            if (decodedImage is null)
            {
                return null;
            }

            int largestEdge = Math.Max(decodedImage.Width, decodedImage.Height);
            double scale = Math.Min(1.0, (double)MaxEdge / largestEdge);
            int width = Math.Max(1, (int)Math.Round(decodedImage.Width * scale));
            int height = Math.Max(1, (int)Math.Round(decodedImage.Height * scale));

            using SKBitmap? resizedImage = scale < 1.0
                ? decodedImage.Resize(
                    new SKImageInfo(width, height),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
                : null;

            SKBitmap image = resizedImage ?? decodedImage;

            if (scale < 1.0 && resizedImage is null)
            {
                return null;
            }

            TuiColor[,] pixels = new TuiColor[image.Width, image.Height];

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    SKColor pixel = image.GetPixel(x, y);

                    // Flatten transparency onto black; terminals have no alpha here.
                    pixels[x, y] = new TuiColor(
                        pixel.Red * pixel.Alpha / 255,
                        pixel.Green * pixel.Alpha / 255,
                        pixel.Blue * pixel.Alpha / 255);
                }
            }

            return pixels;
        }
        catch
        {
            // Unreadable tags or an undecodable picture: simply show no cover.
            return null;
        }
    }
}

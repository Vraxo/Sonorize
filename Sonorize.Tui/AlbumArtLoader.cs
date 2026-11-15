using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
        if (!System.IO.File.Exists(filePath))
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

            using Image<Rgba32> image = Image.Load<Rgba32>(picture.Data.Data);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxEdge, MaxEdge),
                Mode = ResizeMode.Max
            }));

            TuiColor[,] pixels = new TuiColor[image.Width, image.Height];

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba32 pixel = image[x, y];

                    // Flatten transparency onto black; terminals have no alpha here.
                    pixels[x, y] = new TuiColor(
                        pixel.R * pixel.A / 255,
                        pixel.G * pixel.A / 255,
                        pixel.B * pixel.A / 255);
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

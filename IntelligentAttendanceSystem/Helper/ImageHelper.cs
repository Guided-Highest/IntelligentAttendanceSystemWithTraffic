using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace IntelligentAttendanceSystem.Helper
{
    public static class ImageHelper
    {
        public static byte[] CompressImage(byte[] imageData, int quality = 75)
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.Load(inputStream);

            // Resize image if too large (max 500x500)
            if (image.Width > 500 || image.Height > 500)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(500, 500),
                    Mode = ResizeMode.Max
                }));
            }

            // Save with compression
            var encoder = new JpegEncoder { Quality = quality };
            using var outputStream = new MemoryStream();
            image.Save(outputStream, encoder);
            return outputStream.ToArray();
        }
    }
}

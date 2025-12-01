// BarcodeImage.cs (encabezado)
using ZXing;
using ZXing.Common;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
// ⚠️ NO agregues using System.Net.Mime;
// Alias para evitar conflicto:
using Img = SixLabors.ImageSharp.Image;


namespace Muni.Application.Printing;

public static class BarcodeImage
{
    /// Genera PNG del código de barras (Interleaved 2 of 5 o Code128).
    /// width/height controlan el tamaño (en píxeles).
    public static byte[] GeneratePng(string numericCode, bool useITF = true, int width = 800, int height = 180, int margin = 10)
    {
        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = useITF ? BarcodeFormat.ITF : BarcodeFormat.CODE_128, // ITF (Interleaved 2 of 5) o CODE_128
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = margin,
                PureBarcode = true
            }
        };

        // PixelData con formato BGRA32
        var pixelData = writer.Write(numericCode);

        using var img = Img.LoadPixelData<Bgra32>(pixelData.Pixels, pixelData.Width, pixelData.Height);


        // (Opcional) Escalar sutilmente para mejorar legibilidad en impresiones
        // img.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(width, height) }));

        using var ms = new MemoryStream();
        img.Save(ms, new PngEncoder { CompressionLevel = PngCompressionLevel.Level4 });
        return ms.ToArray();
    }
}


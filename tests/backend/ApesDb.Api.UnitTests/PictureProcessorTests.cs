using SkiaSharp;

namespace ApesDb.Api.UnitTests;

public sealed class PictureProcessorTests
{
    private readonly PictureProcessor _processor = new();

    [Fact]
    public void ProcessConvertsPictureToSquareWebp()
    {
        using var input = CreatePng(320, 180);

        var result = _processor.Process(input);

        using var output = SKCodec.Create(new MemoryStream(result));
        Assert.NotNull(output);
        Assert.Equal(SKEncodedImageFormat.Webp, output.EncodedFormat);
        Assert.Equal(PictureProcessor.OutputSize, output.Info.Width);
        Assert.Equal(PictureProcessor.OutputSize, output.Info.Height);
    }

    [Fact]
    public void ProcessRejectsInvalidPicture()
    {
        using var input = new MemoryStream([1, 2, 3]);

        var exception = Assert.Throws<InvalidPictureException>(() => _processor.Process(input));

        Assert.Equal("The picture is not a valid image.", exception.Message);
    }

    [Fact]
    public void ProcessRejectsUnsupportedPictureFormat()
    {
        byte[] gif =
        [
            0x47,
            0x49,
            0x46,
            0x38,
            0x39,
            0x61,
            0x01,
            0x00,
            0x01,
            0x00,
            0x80,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0xFF,
            0xFF,
            0xFF,
            0x21,
            0xF9,
            0x04,
            0x01,
            0x00,
            0x00,
            0x00,
            0x00,
            0x2C,
            0x00,
            0x00,
            0x00,
            0x00,
            0x01,
            0x00,
            0x01,
            0x00,
            0x00,
            0x02,
            0x02,
            0x44,
            0x01,
            0x00,
            0x3B,
        ];
        using var input = new MemoryStream(gif);

        var exception = Assert.Throws<InvalidPictureException>(() => _processor.Process(input));

        Assert.Equal("The picture must be JPEG, PNG, or WebP.", exception.Message);
    }

    private static MemoryStream CreatePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }
}

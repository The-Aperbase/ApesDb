using SkiaSharp;

namespace ApesDb.Api.Features.GamesLists;

public interface IGamesListPictureProcessor
{
    byte[] Process(Stream stream);
}

public sealed class InvalidGamesListPictureException : Exception
{
    public InvalidGamesListPictureException(string message)
        : base(message) { }
}

public sealed class GamesListPictureProcessor : IGamesListPictureProcessor
{
    public const int OutputSize = 256;
    public const long MaximumPixelCount = 25_000_000;

    public byte[] Process(Stream stream)
    {
        using var codec = SKCodec.Create(stream, out var codecResult);
        if (codec is null || codecResult != SKCodecResult.Success)
        {
            throw new InvalidGamesListPictureException("The picture is not a valid image.");
        }

        if (!IsSupportedFormat(codec.EncodedFormat))
        {
            throw new InvalidGamesListPictureException("The picture must be JPEG, PNG, or WebP.");
        }

        var pixelCount = (long)codec.Info.Width * codec.Info.Height;
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || pixelCount > MaximumPixelCount)
        {
            throw new InvalidGamesListPictureException("The picture dimensions are invalid or too large.");
        }

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null)
        {
            throw new InvalidGamesListPictureException("The picture could not be decoded.");
        }

        using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);
        using var output = new SKBitmap(
            new SKImageInfo(OutputSize, OutputSize, SKColorType.Rgba8888, SKAlphaType.Premul)
        );
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);

        var cropSize = Math.Min(oriented.Width, oriented.Height);
        var cropX = (oriented.Width - cropSize) / 2f;
        var cropY = (oriented.Height - cropSize) / 2f;
        var source = new SKRect(cropX, cropY, cropX + cropSize, cropY + cropSize);
        var destination = new SKRect(0, 0, OutputSize, OutputSize);
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
        canvas.DrawBitmap(oriented, source, destination, sampling, null);
        canvas.Flush();

        using var pixmap = output.PeekPixels();
        using var encoded = pixmap.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossy, 80));
        if (encoded is null)
        {
            throw new InvalidGamesListPictureException("The picture could not be encoded.");
        }

        return encoded.ToArray();
    }

    private static bool IsSupportedFormat(SKEncodedImageFormat format)
    {
        return format == SKEncodedImageFormat.Jpeg
            || format == SKEncodedImageFormat.Png
            || format == SKEncodedImageFormat.Webp;
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsDimensions =
            origin == SKEncodedOrigin.LeftTop
            || origin == SKEncodedOrigin.RightTop
            || origin == SKEncodedOrigin.RightBottom
            || origin == SKEncodedOrigin.LeftBottom;
        var width = source.Width;
        var height = source.Height;
        if (swapsDimensions)
        {
            width = source.Height;
            height = source.Width;
        }

        var oriented = new SKBitmap(new SKImageInfo(width, height, source.ColorType, source.AlphaType));
        using var canvas = new SKCanvas(oriented);
        canvas.Clear(SKColors.Transparent);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.Scale(-1, 1);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(width, height);
                canvas.Scale(-1, 1);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(-90);
                break;
        }

        canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear), null);
        canvas.Flush();
        return oriented;
    }
}

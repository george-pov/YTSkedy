using System.Buffers.Binary;
using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class ThumbnailValidatorTests
{
    [Fact]
    public void Validate_Jpeg_ReturnsDimensions()
    {
        var result = ThumbnailValidator.Validate(
            "stream.jpg",
            "image/jpeg",
            Jpeg(width: 640, height: 360));

        Assert.True(result.IsValid);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void Validate_Png_ReturnsDimensions()
    {
        var result = ThumbnailValidator.Validate(
            "stream.png",
            "image/png",
            Png(width: 1280, height: 720));

        Assert.True(result.IsValid);
        Assert.Equal(1280, result.Width);
        Assert.Equal(720, result.Height);
    }

    [Theory]
    [InlineData("stream.gif", "image/png", ThumbnailValidationError.UnsupportedExtension)]
    [InlineData("stream.png", "image/gif", ThumbnailValidationError.UnsupportedContentType)]
    public void Validate_UnsupportedShape_ReturnsError(
        string fileName,
        string contentType,
        ThumbnailValidationError expectedError)
    {
        var result = ThumbnailValidator.Validate(fileName, contentType, Png(2, 2));

        Assert.False(result.IsValid);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void Validate_OverSize_ReturnsTooLarge()
    {
        var result = ThumbnailValidator.Validate(
            "stream.png",
            "image/png",
            new byte[ThumbnailValidator.MaxSizeBytes + 1]);

        Assert.False(result.IsValid);
        Assert.Equal(ThumbnailValidationError.TooLarge, result.Error);
    }

    [Fact]
    public void Validate_UnreadableImage_ReturnsUnreadableImage()
    {
        var result = ThumbnailValidator.Validate(
            "stream.png",
            "image/png",
            [1, 2, 3]);

        Assert.False(result.IsValid);
        Assert.Equal(ThumbnailValidationError.UnreadableImage, result.Error);
    }

    [Fact]
    public void SanitizeFileName_PathValue_ReturnsFileNameOnly()
    {
        var result = ThumbnailValidator.SanitizeFileName(@"C:\temp\Saturday.png");

        Assert.Equal("Saturday.png", result);
    }

    private static byte[] Png(int width, int height)
    {
        var content = new byte[24];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(content, 0);
        "IHDR"u8.CopyTo(content.AsSpan(12, 4));
        BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(16, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(20, 4), height);

        return content;
    }

    private static byte[] Jpeg(int width, int height)
    {
        byte[] content =
        [
            0xFF, 0xD8,
            0xFF, 0xC0,
            0x00, 0x11,
            0x08,
            0x00, 0x00,
            0x00, 0x00,
            0x03,
            0x01, 0x11, 0x00,
            0x02, 0x11, 0x00,
            0x03, 0x11, 0x00,
            0xFF, 0xD9
        ];

        BinaryPrimitives.WriteUInt16BigEndian(content.AsSpan(7, 2), (ushort)height);
        BinaryPrimitives.WriteUInt16BigEndian(content.AsSpan(9, 2), (ushort)width);

        return content;
    }
}

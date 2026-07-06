using System.Buffers.Binary;
using System.IO;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public static class ThumbnailValidator
{
    public const long MaxSizeBytes = 2 * 1024 * 1024;

    private static readonly byte[] PngSignature =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A
    ];

    public static ThumbnailValidationResult Validate(
        string fileName,
        string contentType,
        byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(SanitizeFileName(fileName));
        if (!IsSupportedExtension(extension))
        {
            return ThumbnailValidationResult.Invalid(
                ThumbnailValidationError.UnsupportedExtension);
        }

        if (!IsSupportedContentType(contentType))
        {
            return ThumbnailValidationResult.Invalid(
                ThumbnailValidationError.UnsupportedContentType);
        }

        if (content.LongLength > MaxSizeBytes)
        {
            return ThumbnailValidationResult.Invalid(ThumbnailValidationError.TooLarge);
        }

        return TryReadDimensions(content, contentType, out var width, out var height)
            ? ThumbnailValidationResult.Valid(width, height)
            : ThumbnailValidationResult.Invalid(ThumbnailValidationError.UnreadableImage);
    }

    public static string SanitizeFileName(string fileName)
    {
        var normalized = (fileName ?? string.Empty).Replace('\\', '/');
        var lastSeparator = normalized.LastIndexOf('/');

        return lastSeparator < 0
            ? normalized.Trim()
            : normalized[(lastSeparator + 1)..].Trim();
    }

    private static bool IsSupportedExtension(string extension) =>
        extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".png", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedContentType(string contentType) =>
        contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadDimensions(
        byte[] content,
        string contentType,
        out int width,
        out int height)
    {
        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            return TryReadPngDimensions(content, out width, out height);
        }

        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return TryReadJpegDimensions(content, out width, out height);
        }

        width = 0;
        height = 0;
        return false;
    }

    private static bool TryReadPngDimensions(
        byte[] content,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (content.Length < 24 || !content.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            return false;
        }

        if (!content.AsSpan(12, 4).SequenceEqual("IHDR"u8))
        {
            return false;
        }

        width = BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(16, 4));
        height = BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(20, 4));

        return width > 0 && height > 0;
    }

    private static bool TryReadJpegDimensions(
        byte[] content,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (content.Length < 4 || content[0] != 0xFF || content[1] != 0xD8)
        {
            return false;
        }

        var offset = 2;
        while (offset + 4 <= content.Length)
        {
            while (offset < content.Length && content[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= content.Length)
            {
                return false;
            }

            var marker = content[offset++];
            if (marker is 0xD9 or 0xDA)
            {
                return false;
            }

            if (offset + 2 > content.Length)
            {
                return false;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > content.Length)
            {
                return false;
            }

            if (IsStartOfFrameMarker(marker))
            {
                if (segmentLength < 7)
                {
                    return false;
                }

                height = BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(offset + 3, 2));
                width = BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(offset + 5, 2));

                return width > 0 && height > 0;
            }

            offset += segmentLength;
        }

        return false;
    }

    private static bool IsStartOfFrameMarker(byte marker) =>
        marker is
            0xC0 or
            0xC1 or
            0xC2 or
            0xC3 or
            0xC5 or
            0xC6 or
            0xC7 or
            0xC9 or
            0xCA or
            0xCB or
            0xCD or
            0xCE or
            0xCF;
}

using System.Text.Json;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

internal sealed record ThumbnailJson(
    string FileName,
    string ContentType,
    long SizeBytes,
    int Width,
    int Height,
    DateTimeOffset UpdatedUtc,
    string BlobName)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static ThumbnailJson FromDomain(Thumbnail thumbnail)
    {
        ArgumentNullException.ThrowIfNull(thumbnail);

        return new ThumbnailJson(
            thumbnail.FileName,
            thumbnail.ContentType,
            thumbnail.SizeBytes,
            thumbnail.Width,
            thumbnail.Height,
            thumbnail.UpdatedUtc,
            thumbnail.BlobName);
    }

    internal Thumbnail ToDomain() =>
        new(
            FileName,
            ContentType,
            SizeBytes,
            Width,
            Height,
            UpdatedUtc,
            BlobName);

    internal string Serialize() => JsonSerializer.Serialize(this, JsonOptions);

    internal static Thumbnail? Deserialize(string? json, string calendarEventId)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var thumbnail = JsonSerializer.Deserialize<ThumbnailJson>(
                json,
                JsonOptions) ?? throw new InvalidOperationException(
                $"Calendar event '{calendarEventId}' has missing thumbnail JSON.");

            return thumbnail.ToDomain();
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Calendar event '{calendarEventId}' has malformed thumbnail JSON.",
                exception);
        }
    }
}

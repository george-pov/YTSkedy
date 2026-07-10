using System.Text.Json;

namespace YTSkedy.Infrastructure.CalendarEvents;

internal static class PublishedPlatformIdsJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static string Serialize(IEnumerable<string> platformIds)
    {
        ArgumentNullException.ThrowIfNull(platformIds);

        var normalizedIds = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var platformId in platformIds)
        {
            if (string.IsNullOrWhiteSpace(platformId))
            {
                throw new ArgumentException(
                    "Platform ids must not contain missing or blank values.",
                    nameof(platformIds));
            }

            normalizedIds.Add(platformId);
        }

        return JsonSerializer.Serialize(normalizedIds, JsonOptions);
    }

    internal static IReadOnlySet<string> Deserialize(string? json, string calendarEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                $"Calendar event '{calendarEventId}' has missing or blank published platform ids JSON.");
        }

        try
        {
            var platformIds = JsonSerializer.Deserialize<string?[]>(json, JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Calendar event '{calendarEventId}' has missing published platform ids JSON.");
            var normalizedIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var platformId in platformIds)
            {
                if (string.IsNullOrWhiteSpace(platformId))
                {
                    throw new InvalidOperationException(
                        $"Calendar event '{calendarEventId}' has a missing or blank published platform id.");
                }

                normalizedIds.Add(platformId);
            }

            return normalizedIds;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Calendar event '{calendarEventId}' has malformed published platform ids JSON.",
                exception);
        }
    }
}

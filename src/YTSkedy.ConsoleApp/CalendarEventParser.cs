using System.Globalization;

namespace YTSkedy.ConsoleApp;

internal sealed class CalendarEventParser
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm";
    private const string DateTimeHeader = "date-time";
    private const string EventNameHeader = "event-name";

    public CalendarEventParseResult Parse(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return CalendarEventParseResult.Failure(
                new CalendarEventParseError(1, "Input file is empty."));
        }

        var headers = SplitLine(lines[0]);

        if (headers.Length != 2 ||
            !string.Equals(headers[0], DateTimeHeader, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(headers[1], EventNameHeader, StringComparison.OrdinalIgnoreCase))
        {
            return CalendarEventParseResult.Failure(
                new CalendarEventParseError(
                    1,
                    $"Header must be exactly: {DateTimeHeader},{EventNameHeader}."));
        }

        List<CalendarEventInput> events = [];

        for (var lineIndex = 1; lineIndex < lines.Count; lineIndex++)
        {
            var rowNumber = lineIndex + 1;
            var columns = SplitLine(lines[lineIndex]);

            if (columns.Length != 2)
            {
                return CalendarEventParseResult.Failure(
                    new CalendarEventParseError(
                        rowNumber,
                        "Row must contain exactly 2 columns: date-time,event-name."));
            }

            var dateTimeValue = columns[0];
            var eventName = columns[1];

            if (!DateTime.TryParseExact(
                    dateTimeValue,
                    DateTimeFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDateTime))
            {
                return CalendarEventParseResult.Failure(
                    new CalendarEventParseError(
                        rowNumber,
                        $"date-time must use format {DateTimeFormat}."));
            }

            if (string.IsNullOrWhiteSpace(eventName))
            {
                return CalendarEventParseResult.Failure(
                    new CalendarEventParseError(rowNumber, "event-name is required."));
            }

            events.Add(new CalendarEventInput(parsedDateTime, eventName));
        }

        return CalendarEventParseResult.Success(events);
    }

    private static string[] SplitLine(string line)
    {
        return line.Split(',').Select(column => column.Trim()).ToArray();
    }
}

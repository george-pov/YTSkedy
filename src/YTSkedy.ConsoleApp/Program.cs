using System.Globalization;

namespace YTSkedy.ConsoleApp
{
    internal class Program
    {
        private const string DateTimeOutputFormat = "yyyy-MM-dd HH:mm";

        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                PrintUsage();
                return 1;
            }

            var calendarEventsPath = args[0];

            if (!File.Exists(calendarEventsPath))
            {
                Console.Error.WriteLine($"Input file not found: {calendarEventsPath}");
                PrintUsage();
                return 1;
            }

            string[] lines;

            try
            {
                lines = File.ReadAllLines(calendarEventsPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Unable to read input file: {exception.Message}");
                return 1;
            }

            CalendarEventParser parser = new();
            var result = parser.Parse(lines);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    var location = error.RowNumber is null ? "Input" : $"Row {error.RowNumber}";
                    Console.Error.WriteLine($"{location}: {error.Message}");
                }

                return 1;
            }

            Console.WriteLine("Calendar events:");

            foreach (var calendarEvent in result.Events)
            {
                Console.WriteLine(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{calendarEvent.DateTime:yyyy-MM-dd HH:mm} - {calendarEvent.EventName}"));
            }

            return 0;
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine("Usage: YTSkedy.ConsoleApp <calendar-events.csv>");
            Console.Error.WriteLine("Required columns: date-time,event-name");
            Console.Error.WriteLine($"Accepted format: date-time={DateTimeOutputFormat}");
        }
    }
}

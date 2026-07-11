namespace YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

public sealed class CategoryReadException : Exception
{
    public CategoryReadException(string message)
        : base(message)
    {
    }

    public CategoryReadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

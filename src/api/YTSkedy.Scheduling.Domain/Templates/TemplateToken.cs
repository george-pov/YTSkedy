namespace YTSkedy.Scheduling.Domain.Templates;

/// <summary>
/// A code-defined placeholder token that template content can reference, for
/// example <c>{{ longDate }}</c>. <see cref="Name"/> is the token
/// identifier without the surrounding braces. See
/// <see cref="TemplateTokenCatalog"/> for the single source of the available
/// tokens.
/// </summary>
public sealed record TemplateToken(string Name);

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Outcome of a create template attempt as seen by the HTTP host.
/// <see cref="TemplateId"/> is set only when <see cref="Status"/> is
/// <see cref="CreateTemplateStatus.Created"/>, which maps to 200.
/// <see cref="CreateTemplateStatus.NameAlreadyExists"/> maps to 409 because the
/// name is already used within the type.
/// </summary>
public sealed record CreateTemplateResult(
    CreateTemplateStatus Status,
    string? TemplateId)
{
    public static CreateTemplateResult Created(string templateId) =>
        new(CreateTemplateStatus.Created, templateId);

    public static CreateTemplateResult NameAlreadyExists() =>
        new(CreateTemplateStatus.NameAlreadyExists, null);
}

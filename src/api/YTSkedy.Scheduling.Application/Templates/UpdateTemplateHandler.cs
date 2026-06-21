namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Updates the name and content of an existing template located by type and id.
/// The repository owns the locate, the name-uniqueness check on rename, and the
/// not-found outcome, so the handler forwards the command and returns the
/// repository result unchanged.
/// </summary>
public sealed class UpdateTemplateHandler(ITemplateRepository templates)
{
    public async Task<UpdateTemplateResult> HandleAsync(
        UpdateTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await templates.UpdateAsync(
            command.Type,
            command.Id,
            command.Name,
            command.Content,
            cancellationToken);
    }
}

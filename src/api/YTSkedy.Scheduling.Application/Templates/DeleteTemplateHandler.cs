namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Deletes a template located by type and id. The repository owns the locate and
/// the not-found outcome, so the handler forwards the command and returns the
/// repository result unchanged.
/// </summary>
public sealed class DeleteTemplateHandler(ITemplateModifier templates)
{
    public async Task<DeleteTemplateResult> HandleAsync(
        DeleteTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await templates.DeleteAsync(
            command.Type,
            command.Id,
            cancellationToken);
    }
}

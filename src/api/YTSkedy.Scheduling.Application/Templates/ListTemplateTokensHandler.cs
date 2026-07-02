using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Returns the template token catalog for the current event text field list and
/// active platform reference keys. The handler keeps the HTTP host behind one
/// use-case entry point while the domain catalog owns token ordering and fixed
/// date tokens.
/// </summary>
public sealed class ListTemplateTokensHandler(
    IEventTextFieldsReader eventTextFields,
    IPlatformReader platforms)
{
    public async Task<IReadOnlyList<TemplateToken>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var fields = await eventTextFields.GetAsync(cancellationToken);
        var activePlatforms = await platforms.ListAsync(null, cancellationToken);

        return TemplateTokenCatalog.From(
            fields,
            activePlatforms.Select(platform => platform.ReferenceKey));
    }
}

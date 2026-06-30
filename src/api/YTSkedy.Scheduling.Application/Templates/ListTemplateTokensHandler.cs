using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Returns the template token catalog for the current event text field list.
/// The handler keeps the HTTP host behind one use-case entry point while the
/// domain catalog owns token ordering and fixed date tokens.
/// </summary>
public sealed class ListTemplateTokensHandler(IEventTextFieldsReader eventTextFields)
{
    public async Task<IReadOnlyList<TemplateToken>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var fields = await eventTextFields.GetAsync(cancellationToken);

        return TemplateTokenCatalog.From(fields);
    }
}

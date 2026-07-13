using YTSkedy.Infrastructure.Settings;

namespace YTSkedy.Infrastructure.Test.Settings;

public sealed class ApplicationSettingsKeyTests
{
    [Fact]
    public void Constants_EventTextFields_UseGenericApplicationSettingsKey()
    {
        Assert.Equal("application-settings", ApplicationSettingsKey.PartitionKey);
        Assert.Equal("event-text-fields", ApplicationSettingsKey.EventTextFieldsRowKey);
        Assert.Equal("calendar-event-start-defaults", ApplicationSettingsKey.StartDefaultsRowKey);
    }
}

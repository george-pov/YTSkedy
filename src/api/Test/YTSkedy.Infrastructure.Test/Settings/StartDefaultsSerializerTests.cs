using System.Text.Json;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.Settings;

public sealed class StartDefaultsSerializerTests
{
    [Fact]
    public void Serialize_FullDefaults_WritesCanonicalStrings()
    {
        var json = StartDefaultsSerializer.Serialize(
            new StartDefaults(DayOfWeek.Wednesday, new TimeOnly(9, 5), "America/Vancouver"));

        using var document = JsonDocument.Parse(json);
        Assert.Equal("Wednesday", document.RootElement.GetProperty("dayOfWeek").GetString());
        Assert.Equal("09:05", document.RootElement.GetProperty("localTime").GetString());
        Assert.Equal("America/Vancouver", document.RootElement.GetProperty("timeZoneId").GetString());
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyDefaults_RoundTripsNulls()
    {
        var result = StartDefaultsSerializer.Deserialize(
            StartDefaultsSerializer.Serialize(StartDefaults.Empty));

        Assert.Equal(StartDefaults.Empty, result);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"dayOfWeek\":\"monday\",\"localTime\":null,\"timeZoneId\":null}")]
    [InlineData("{\"dayOfWeek\":null,\"localTime\":\"9:00\",\"timeZoneId\":null}")]
    [InlineData("{\"dayOfWeek\":null,\"localTime\":null,\"timeZoneId\":\"Unknown/Zone\"}")]
    public void Deserialize_MalformedStoredState_Throws(string json) =>
        Assert.Throws<InvalidOperationException>(() => StartDefaultsSerializer.Deserialize(json));
}

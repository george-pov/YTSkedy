namespace YTSkedy.Infrastructure.IntegrationTest.TestSupport;

[CollectionDefinition(Name)]
public sealed class AzuriteTableCollection : ICollectionFixture<AzuriteTableFixture>
{
    public const string Name = "Azurite tables";
}

using YTSkedy.AzureFunctions.Configuration;

namespace YTSkedy.AzureFunctions.Test.Configuration;

public sealed class AzureStorageClientFactoryTests
{
    [Fact]
    public void CreateClients_ConnectionStringMode_UsesDevelopmentEndpoints()
    {
        var options = new AzureStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true"
        };

        var table = AzureStorageClientFactory.CreateTableServiceClient(options);
        var blob = AzureStorageClientFactory.CreateBlobServiceClient(options);

        Assert.Equal("127.0.0.1", table.Uri.Host);
        Assert.Equal(10002, table.Uri.Port);
        Assert.Equal("127.0.0.1", blob.Uri.Host);
        Assert.Equal(10000, blob.Uri.Port);
    }

    [Fact]
    public void CreateClients_ServiceUriMode_UsesConfiguredEndpoints()
    {
        var options = new AzureStorageOptions
        {
            TableServiceUri = "https://tables.example.test/",
            BlobServiceUri = "https://blobs.example.test/"
        };

        var table = AzureStorageClientFactory.CreateTableServiceClient(options);
        var blob = AzureStorageClientFactory.CreateBlobServiceClient(options);

        Assert.Equal(new Uri("https://tables.example.test/"), table.Uri);
        Assert.Equal(new Uri("https://blobs.example.test/"), blob.Uri);
    }
}

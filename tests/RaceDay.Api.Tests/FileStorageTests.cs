using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>
/// Part 3 checks for the storage switch: Jarred's banner uploads go to Azure Blob Storage when a connection
/// string is configured, and to local disk when it is not (which is what CI uses).
/// </summary>
public class FileStorageTests
{
    [Fact]
    public void Without_a_blob_connection_string_the_api_uses_local_storage()
    {
        using var factory = new RaceDayWebApplicationFactory();

        var storage = factory.Services.GetRequiredService<IFileStorage>();

        Assert.IsType<LocalFileStorage>(storage);
    }

    [Fact]
    public void With_a_blob_connection_string_the_api_uses_azure_blob_storage()
    {
        // The development-storage connection string is enough to build the client. Nothing is uploaded here.
        using var factory = new RaceDayWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.UseSetting("AzureBlob:ConnectionString", "UseDevelopmentStorage=true"));

        var storage = factory.Services.GetRequiredService<IFileStorage>();

        Assert.IsType<BlobFileStorage>(storage);
    }

    [Theory]
    [InlineData(".png", "image/png")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    public void Blob_uploads_get_the_right_content_type(string extension, string expected)
    {
        Assert.Equal(expected, BlobFileStorage.ContentTypeFor(extension));
    }
}

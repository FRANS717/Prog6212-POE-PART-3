using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Part 3 storage: saves uploaded images to an Azure Blob Storage container and returns the blob's public URL.
/// The controllers only know <see cref="IFileStorage"/>, so swapping local disk for Azure needed no controller changes.
/// The MVC app never talks to Azure. It sends the file to the API, and the API stores it here.
/// </summary>
public class BlobFileStorage(BlobContainerClient container) : IFileStorage
{
    private bool _containerChecked;

    public async Task<string> SaveImageAsync(IFormFile file, string folder, CancellationToken ct)
    {
        await EnsureContainerAsync(ct);

        // A random name stops one upload overwriting another. The folder becomes a virtual directory, e.g. banners/abc.jpg.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            // Setting the content type lets the browser show the image instead of downloading it.
            HttpHeaders = new BlobHttpHeaders { ContentType = ContentTypeFor(extension) }
        }, ct);

        return blob.Uri.ToString();
    }

    /// <summary>Creates the container on first use, with public read access to blobs only (not the list of blobs).</summary>
    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerChecked) return;
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        _containerChecked = true;
    }

    public static string ContentTypeFor(string extension) => extension switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}

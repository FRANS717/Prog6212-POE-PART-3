using Microsoft.Extensions.Options;

namespace RaceDay.Web.Services;

/// <summary>Where the API lives. BaseUrl is used by the server; PublicBaseUrl is what the browser can reach.</summary>
public class ApiOptions
{
    public const string Section = "Api";

    /// <summary>URL the MVC server calls, e.g. http://localhost:5043, or http://host.docker.internal:5043 inside Docker.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5043";

    /// <summary>URL the browser uses for API-hosted images. Defaults to BaseUrl.</summary>
    public string? PublicBaseUrl { get; set; }
}

/// <summary>
/// Turns an image URL from the API into one the browser can load. Azure Blob URLs are already absolute
/// and are used as they are. Local-disk URLs from the Part 2 fallback (/uploads/...) get the API address in front.
/// </summary>
public class ImageUrls(IOptions<ApiOptions> options)
{
    private readonly string _publicBase = (options.Value.PublicBaseUrl ?? options.Value.BaseUrl).TrimEnd('/');

    public string? For(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        // I check the scheme instead of Uri.TryCreate, because on Linux (Docker) "/uploads/x.jpg" parses as an absolute file URI.
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;
        return _publicBase + "/" + url.TrimStart('/');
    }
}

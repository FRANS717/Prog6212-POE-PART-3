using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>An API login or register result, plus the API session cookie returned by login.</summary>
public class AuthResult : ApiResult<AuthResponse>
{
    public string? ApiCookie { get; init; }
}

/// <summary>
/// Typed HttpClient for the Part 2 RaceDay API. It is the only place the MVC app gets data from.
/// The API keeps its own server-side session, identified by the "RaceDay.Session" cookie. After login
/// I store that cookie in the MVC session and attach it to every request, so the API sees the right user
/// and enforces the role rules itself. The MVC app never decides access on data it fetched.
/// </summary>
public class RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApiClient> logger) : IRaceDayApi
{
    public const string ApiCookieName = "RaceDay.Session";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---------------- Authentication ----------------

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var result = await SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", JsonContent.Create(request, options: Json), ct);
        return new AuthResult { Success = result.Success, StatusCode = result.StatusCode, Error = result.Error, Data = result.Data };
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = JsonContent.Create(request, options: Json)
            };
            using var response = await http.SendAsync(message, ct);

            if (!response.IsSuccessStatusCode)
            {
                return new AuthResult { StatusCode = response.StatusCode, Error = await ReadErrorAsync(response, ct) };
            }

            // The API sets its session cookie on a successful login. I keep only the value for later requests.
            var cookie = ReadApiCookie(response);
            var data = await response.Content.ReadFromJsonAsync<AuthResponse>(Json, ct);
            return new AuthResult { Success = cookie is not null, StatusCode = response.StatusCode, Data = data, ApiCookie = cookie,
                Error = cookie is null ? "The RaceDay service did not start a session. Please try again." : null };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Login request to the API failed");
            return new AuthResult { StatusCode = HttpStatusCode.ServiceUnavailable, Error = ServiceDownMessage };
        }
    }

    public Task<ApiResult> LogoutAsync(CancellationToken ct) =>
        SendAsync(HttpMethod.Post, "api/auth/logout", null, ct, throwOnExpiredSession: false);

    // ---------------- Profile ----------------

    public Task<ApiResult<UserProfile>> GetProfileAsync(CancellationToken ct) =>
        SendAsync<UserProfile>(HttpMethod.Get, "api/users/profile", null, ct);

    public Task<ApiResult<UserProfile>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct) =>
        SendAsync<UserProfile>(HttpMethod.Put, "api/users/profile", JsonContent.Create(request, options: Json), ct);

    public Task<ApiResult<ImageUpload>> UploadProfilePictureAsync(IFormFile image, CancellationToken ct) =>
        SendAsync<ImageUpload>(HttpMethod.Post, "api/users/profile/picture", ImageContent(image), ct);

    // ---------------- Events ----------------

    public Task<ApiResult<List<EventSummary>>> GetEventsAsync(string? type, DateOnly? date, string? location, bool mine, CancellationToken ct)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");
        if (date is not null) query.Add($"date={date.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(location)) query.Add($"location={Uri.EscapeDataString(location.Trim())}");
        if (mine) query.Add("mine=true");

        var url = query.Count == 0 ? "api/events" : $"api/events?{string.Join('&', query)}";
        return SendAsync<List<EventSummary>>(HttpMethod.Get, url, null, ct);
    }

    public Task<ApiResult<EventDetail>> GetEventAsync(int id, CancellationToken ct) =>
        SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{id}", null, ct);

    public Task<ApiResult<EventDetail>> CreateEventAsync(EventRequest request, CancellationToken ct) =>
        SendAsync<EventDetail>(HttpMethod.Post, "api/events", JsonContent.Create(request, options: Json), ct);

    public Task<ApiResult<EventDetail>> UpdateEventAsync(int id, EventRequest request, CancellationToken ct) =>
        SendAsync<EventDetail>(HttpMethod.Put, $"api/events/{id}", JsonContent.Create(request, options: Json), ct);

    public Task<ApiResult> DeleteEventAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Delete, $"api/events/{id}", null, ct);

    public Task<ApiResult<ImageUpload>> UploadBannerAsync(int eventId, IFormFile image, CancellationToken ct) =>
        SendAsync<ImageUpload>(HttpMethod.Post, $"api/events/{eventId}/upload-banner", ImageContent(image), ct);

    // ---------------- Categories ----------------

    public Task<ApiResult<List<Category>>> GetCategoriesAsync(int eventId, CancellationToken ct) =>
        SendAsync<List<Category>>(HttpMethod.Get, $"api/events/{eventId}/categories", null, ct);

    public Task<ApiResult<Category>> CreateCategoryAsync(int eventId, CategoryRequest request, CancellationToken ct) =>
        SendAsync<Category>(HttpMethod.Post, $"api/events/{eventId}/categories", JsonContent.Create(request, options: Json), ct);

    public Task<ApiResult<Category>> UpdateCategoryAsync(int id, CategoryRequest request, CancellationToken ct) =>
        SendAsync<Category>(HttpMethod.Put, $"api/categories/{id}", JsonContent.Create(request, options: Json), ct);

    public Task<ApiResult> DeleteCategoryAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Delete, $"api/categories/{id}", null, ct);

    // ---------------- Enrolments ----------------

    public Task<ApiResult<List<Enrolment>>> GetMyEnrolmentsAsync(CancellationToken ct) =>
        SendAsync<List<Enrolment>>(HttpMethod.Get, "api/enrolments", null, ct);

    public Task<ApiResult<List<Enrolment>>> GetEventEnrolmentsAsync(int eventId, CancellationToken ct) =>
        SendAsync<List<Enrolment>>(HttpMethod.Get, $"api/events/{eventId}/enrolments", null, ct);

    public Task<ApiResult<Enrolment>> EnrolAsync(int eventId, int categoryId, CancellationToken ct) =>
        SendAsync<Enrolment>(HttpMethod.Post, $"api/events/{eventId}/enrol", JsonContent.Create(new EnrolRequest(categoryId), options: Json), ct);

    public Task<ApiResult<Enrolment>> UpdateEnrolmentStatusAsync(int id, string status, CancellationToken ct) =>
        SendAsync<Enrolment>(HttpMethod.Put, $"api/enrolments/{id}/status", JsonContent.Create(new UpdateEnrolmentStatusRequest(status), options: Json), ct);

    // ---------------- Results ----------------

    public Task<ApiResult<List<RaceResult>>> GetMyResultsAsync(CancellationToken ct) =>
        SendAsync<List<RaceResult>>(HttpMethod.Get, "api/results/me", null, ct);

    public Task<ApiResult<List<RaceResult>>> GetEventResultsAsync(int eventId, CancellationToken ct) =>
        SendAsync<List<RaceResult>>(HttpMethod.Get, $"api/events/{eventId}/results", null, ct);

    public Task<ApiResult<RaceResult>> CaptureResultAsync(int eventId, CaptureResultRequest request, CancellationToken ct) =>
        SendAsync<RaceResult>(HttpMethod.Post, $"api/events/{eventId}/results", JsonContent.Create(request, options: Json), ct);

    public Task<ApiResult<RaceResult>> UpdateResultAsync(int id, UpdateResultRequest request, CancellationToken ct) =>
        SendAsync<RaceResult>(HttpMethod.Put, $"api/results/{id}", JsonContent.Create(request, options: Json), ct);

    // ---------------- Plumbing ----------------

    public const string ServiceDownMessage = "The RaceDay service is not reachable right now. Please try again in a moment.";

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
    {
        var (response, failure) = await SendRawAsync(method, url, content, ct, throwOnExpiredSession: true);
        if (failure is not null) return new ApiResult<T> { StatusCode = failure.StatusCode, Error = failure.Error };

        using (response)
        {
            if (!response!.IsSuccessStatusCode)
                return new ApiResult<T> { StatusCode = response.StatusCode, Error = await ReadErrorAsync(response, ct) };

            var data = response.StatusCode == HttpStatusCode.NoContent
                ? default
                : await response.Content.ReadFromJsonAsync<T>(Json, ct);
            return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
        }
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct, bool throwOnExpiredSession = true)
    {
        var (response, failure) = await SendRawAsync(method, url, content, ct, throwOnExpiredSession);
        if (failure is not null) return failure;

        using (response)
        {
            return response!.IsSuccessStatusCode
                ? new ApiResult { Success = true, StatusCode = response.StatusCode }
                : new ApiResult { StatusCode = response.StatusCode, Error = await ReadErrorAsync(response, ct) };
        }
    }

    private async Task<(HttpResponseMessage? Response, ApiResult? Failure)> SendRawAsync(
        HttpMethod method, string url, HttpContent? content, CancellationToken ct, bool throwOnExpiredSession)
    {
        var session = accessor.HttpContext?.Session;
        var apiCookie = session?.ApiCookie();

        using var message = new HttpRequestMessage(method, url) { Content = content };
        if (apiCookie is not null)
        {
            // Forward the logged-in user's API session. The handler has UseCookies = false, so this header is sent as-is.
            message.Headers.Add("Cookie", $"{ApiCookieName}={apiCookie}");
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "API request {Method} {Url} failed", method, url);
            return (null, new ApiResult { StatusCode = HttpStatusCode.ServiceUnavailable, Error = ServiceDownMessage });
        }

        // The user is logged in here, but the API no longer knows the session (it expired or the API restarted).
        if (response.StatusCode == HttpStatusCode.Unauthorized && apiCookie is not null && throwOnExpiredSession)
        {
            response.Dispose();
            throw new ApiSessionExpiredException();
        }

        return (response, null);
    }

    /// <summary>Reads the API's { "message": "..." } error body. Falls back to a plain message, never to raw content.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(Json, ct);
            if (!string.IsNullOrWhiteSpace(error?.Message)) return error.Message;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            // Not our JSON error shape (for example an empty 404). Use the generic message below.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => "That item could not be found.",
            HttpStatusCode.Forbidden => "You do not have access to that.",
            HttpStatusCode.Unauthorized => "Please log in first.",
            _ => "Something went wrong. Please check your input and try again."
        };
    }

    /// <summary>Finds the value of the API session cookie in a login response's Set-Cookie headers.</summary>
    public static string? ReadApiCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;

        foreach (var header in cookies)
        {
            var pair = header.Split(';', 2)[0].Trim();
            if (pair.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
                return pair[(ApiCookieName.Length + 1)..];
        }
        return null;
    }

    /// <summary>Wraps an uploaded file as multipart/form-data in the "image" field the API expects.</summary>
    private static MultipartFormDataContent ImageContent(IFormFile image)
    {
        var file = new StreamContent(image.OpenReadStream());
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType);
        return new MultipartFormDataContent { { file, "image", Path.GetFileName(image.FileName) } };
    }
}

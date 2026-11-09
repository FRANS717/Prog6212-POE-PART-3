using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>Every RaceDay API call the MVC app makes. One method per endpoint in the Part 1 plan.</summary>
public interface IRaceDayApi
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<ApiResult> LogoutAsync(CancellationToken ct);

    Task<ApiResult<UserProfile>> GetProfileAsync(CancellationToken ct);
    Task<ApiResult<UserProfile>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct);
    Task<ApiResult<ImageUpload>> UploadProfilePictureAsync(IFormFile image, CancellationToken ct);

    Task<ApiResult<List<EventSummary>>> GetEventsAsync(string? type, DateOnly? date, string? location, bool mine, CancellationToken ct);
    Task<ApiResult<EventDetail>> GetEventAsync(int id, CancellationToken ct);
    Task<ApiResult<EventDetail>> CreateEventAsync(EventRequest request, CancellationToken ct);
    Task<ApiResult<EventDetail>> UpdateEventAsync(int id, EventRequest request, CancellationToken ct);
    Task<ApiResult> DeleteEventAsync(int id, CancellationToken ct);
    Task<ApiResult<ImageUpload>> UploadBannerAsync(int eventId, IFormFile image, CancellationToken ct);

    Task<ApiResult<List<Category>>> GetCategoriesAsync(int eventId, CancellationToken ct);
    Task<ApiResult<Category>> CreateCategoryAsync(int eventId, CategoryRequest request, CancellationToken ct);
    Task<ApiResult<Category>> UpdateCategoryAsync(int id, CategoryRequest request, CancellationToken ct);
    Task<ApiResult> DeleteCategoryAsync(int id, CancellationToken ct);

    Task<ApiResult<List<Enrolment>>> GetMyEnrolmentsAsync(CancellationToken ct);
    Task<ApiResult<List<Enrolment>>> GetEventEnrolmentsAsync(int eventId, CancellationToken ct);
    Task<ApiResult<Enrolment>> EnrolAsync(int eventId, int categoryId, CancellationToken ct);
    Task<ApiResult<Enrolment>> UpdateEnrolmentStatusAsync(int id, string status, CancellationToken ct);

    Task<ApiResult<List<RaceResult>>> GetMyResultsAsync(CancellationToken ct);
    Task<ApiResult<List<RaceResult>>> GetEventResultsAsync(int eventId, CancellationToken ct);
    Task<ApiResult<RaceResult>> CaptureResultAsync(int eventId, CaptureResultRequest request, CancellationToken ct);
    Task<ApiResult<RaceResult>> UpdateResultAsync(int id, UpdateResultRequest request, CancellationToken ct);
}

namespace RaceDay.Web.Models.Api;

// These records mirror the JSON contracts of the Part 2 API (src/RaceDay.Api/Contracts).
// The MVC project keeps its own copies on purpose: it talks to the API over HTTP only,
// so it does not reference the API project, EF Core or the database.

public record AuthResponse(int UserId, string Email, string Role);

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

public record LoginRequest(string Email, string Password);

public record UserProfile(
    int UserId,
    string Email,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth,
    string? ProfilePictureUrl);

public record UpdateProfileRequest(
    string FullName,
    string Email,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

public record EventSummary(
    int EventId,
    string Name,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl,
    int OrganiserId,
    string OrganisationName);

public record EventDetail(
    int EventId,
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl,
    int OrganiserId,
    string OrganisationName,
    IReadOnlyList<Category> Categories);

public record EventRequest(
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl);

public record Category(int CategoryId, int EventId, string Name, string? Description, int? MinAge, int? MaxAge);

public record CategoryRequest(string Name, string? Description, int? MinAge, int? MaxAge);

public record Enrolment(
    int EnrolmentId,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    int ParticipantId,
    string ParticipantName,
    DateTime RegistrationDate,
    string Status);

public record EnrolRequest(int CategoryId);

public record UpdateEnrolmentStatusRequest(string Status);

public record RaceResult(
    int ResultId,
    int EnrolmentId,
    int EventId,
    string EventName,
    DateTime EventDate,
    string CategoryName,
    string ParticipantName,
    TimeSpan FinishTime,
    int FinishPosition,
    int TotalFinishers,
    string Status);

public record CaptureResultRequest(int EnrolmentId, TimeSpan FinishTime, int FinishPosition);

public record UpdateResultRequest(TimeSpan FinishTime, int FinishPosition);

public record ImageUpload(string ImageUrl);

public record ApiError(string Message);

/// <summary>Allowed values shared with the API, used to build drop-downs and badges.</summary>
public static class RaceDayValues
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] EventTypes = ["Run", "Walk", "Cycle"];
    public static readonly string[] EnrolmentStatuses = ["Pending", "Confirmed", "Cancelled"];
}

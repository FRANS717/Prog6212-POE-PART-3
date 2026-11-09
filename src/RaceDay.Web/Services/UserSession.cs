namespace RaceDay.Web.Services;

/// <summary>
/// Reads and writes the logged-in user's details in the MVC server-side session.
/// The session keeps the user ID, role, name and the API's own session cookie, so every
/// page knows who is logged in and every API call is made as that user.
/// </summary>
public static class UserSession
{
    public const string UserIdKey = "UserId";
    public const string RoleKey = "Role";
    public const string EmailKey = "Email";
    public const string NameKey = "FullName";
    public const string ApiCookieKey = "ApiCookie";

    public static int? UserId(this ISession session) => session.GetInt32(UserIdKey);
    public static string? Role(this ISession session) => session.GetString(RoleKey);
    public static string? Email(this ISession session) => session.GetString(EmailKey);
    public static string? FullName(this ISession session) => session.GetString(NameKey);
    public static string? ApiCookie(this ISession session) => session.GetString(ApiCookieKey);

    public static bool IsLoggedIn(this ISession session) => session.UserId() is not null && session.Role() is not null;
    public static bool IsOrganiser(this ISession session) => session.Role() == Models.Api.RaceDayValues.Organiser;
    public static bool IsParticipant(this ISession session) => session.Role() == Models.Api.RaceDayValues.Participant;

    public static void SignIn(this ISession session, int userId, string email, string role, string fullName, string apiCookie)
    {
        session.SetInt32(UserIdKey, userId);
        session.SetString(EmailKey, email);
        session.SetString(RoleKey, role);
        session.SetString(NameKey, fullName);
        session.SetString(ApiCookieKey, apiCookie);
    }

    public static void UpdateName(this ISession session, string fullName, string email)
    {
        session.SetString(NameKey, fullName);
        session.SetString(EmailKey, email);
    }
}

namespace RaceDay.Web.Services;

/// <summary>
/// Thrown when the MVC app thinks the user is logged in but the API answers 401, for example because
/// the API was restarted and lost its session. <see cref="Filters.ApiSessionExpiredFilter"/> catches it,
/// clears the MVC session and sends the user back to the login page with a message.
/// </summary>
public class ApiSessionExpiredException() : Exception("The API session has expired.");

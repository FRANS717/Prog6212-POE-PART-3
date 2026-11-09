using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects MVC pages using the session. With no session the user is sent to the login page (and returned to the
/// page afterwards). With the wrong role they get the Access Denied page.
/// Example: [RequireRole(RaceDayValues.Organiser)] on the organiser controllers.
/// The API still checks every request too, so this is about navigation, not the only line of defence.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public IReadOnlyList<string> Roles { get; } = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Path + request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (Roles.Count > 0 && !Roles.Contains(session.Role()))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

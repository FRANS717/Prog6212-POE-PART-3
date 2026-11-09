using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Global filter: when the API says the session has expired, I clear the MVC session too and send the user
/// to the login page with a short explanation, instead of showing an error page.
/// </summary>
public class ApiSessionExpiredFilter(ITempDataDictionaryFactory tempDataFactory) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ApiSessionExpiredException) return;

        var http = context.HttpContext;
        http.Session.Clear();

        var tempData = tempDataFactory.GetTempData(http);
        tempData["Warning"] = "Your session has expired. Please log in again.";

        var returnUrl = http.Request.Method == HttpMethods.Get ? http.Request.Path + http.Request.QueryString : null;
        context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        context.ExceptionHandled = true;
    }
}

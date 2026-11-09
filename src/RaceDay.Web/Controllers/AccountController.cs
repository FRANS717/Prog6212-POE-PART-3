using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in and log out. Every check of the credentials happens in the API.</summary>
public class AccountController(IRaceDayApi api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (HttpContext.Session.IsLoggedIn()) return RedirectToHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var error = await SignInAsync(model.Email, model.Password, ct);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            model.Password = "";
            return View(model);
        }

        TempData["Success"] = $"Welcome back, {HttpContext.Session.FullName()}!";

        // Only follow local return URLs, so a crafted link cannot send the user to another site after login.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
        return RedirectToHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn()) return RedirectToHome();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var isOrganiser = model.Role == RaceDayValues.Organiser;
        var request = new RegisterRequest(
            model.Email.Trim(),
            model.Password,
            model.FullName.Trim(),
            model.Role,
            isOrganiser ? model.OrganisationName?.Trim() : null,
            isOrganiser ? model.ContactNumber?.Trim() : null,
            isOrganiser ? null : model.DateOfBirth);

        var result = await api.RegisterAsync(request, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return View(model);
        }

        // The API's register does not start a session, so I log the new user in straight away.
        var error = await SignInAsync(model.Email, model.Password, ct);
        if (error is not null)
        {
            TempData["Success"] = "Your account was created. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        TempData["Success"] = $"Welcome to RaceDay, {HttpContext.Session.FullName()}!";
        return RedirectToHome();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // End the API session first, then the MVC one. If the API session was already gone that is fine.
        if (HttpContext.Session.IsLoggedIn()) await api.LogoutAsync(ct);
        HttpContext.Session.Clear();

        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    /// <summary>Logs in through the API and fills the MVC session. Returns an error message, or null on success.</summary>
    private async Task<string?> SignInAsync(string email, string password, CancellationToken ct)
    {
        var login = await api.LoginAsync(new LoginRequest(email.Trim(), password), ct);
        if (!login.Success || login.Data is null || login.ApiCookie is null)
            return login.Error ?? "Login failed.";

        HttpContext.Session.Clear();
        HttpContext.Session.SignIn(login.Data.UserId, login.Data.Email, login.Data.Role, login.Data.Email, login.ApiCookie);

        // The login response has no name, so I read the profile once to greet the user by name in the menu.
        var profile = await api.GetProfileAsync(ct);
        if (profile.Success && profile.Data is not null)
            HttpContext.Session.UpdateName(profile.Data.FullName, profile.Data.Email);

        return null;
    }

    private IActionResult RedirectToHome() =>
        HttpContext.Session.IsOrganiser()
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
}

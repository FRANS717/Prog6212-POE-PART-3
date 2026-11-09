using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>View and update your own profile (both roles). Participants can also upload a profile picture.</summary>
[RequireRole]
public class ProfileController(IRaceDayApi api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await api.GetProfileAsync(ct);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }

        return View(ProfileViewModel.From(result.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken ct)
    {
        // The role and picture are not posted by the form, so I take them from the session and the API.
        model.Role = HttpContext.Session.Role()!;
        ModelState.Clear();
        TryValidateModel(model);

        if (!ModelState.IsValid) return await RedisplayAsync(model, ct);

        var request = new UpdateProfileRequest(
            model.FullName.Trim(),
            model.Email.Trim(),
            model.IsOrganiser ? model.OrganisationName?.Trim() : null,
            model.IsOrganiser ? model.ContactNumber?.Trim() : null,
            model.IsOrganiser ? null : model.DateOfBirth);

        var result = await api.UpdateProfileAsync(request, ct);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Your profile could not be saved.");
            return await RedisplayAsync(model, ct);
        }

        HttpContext.Session.UpdateName(result.Data.FullName, result.Data.Email);
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sends the picture to the API, which validates it and stores it in Azure Blob Storage.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole(RaceDayValues.Participant)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Picture(IFormFile? image, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
        {
            TempData["Error"] = "Choose a picture to upload.";
            return RedirectToAction(nameof(Index));
        }

        var result = await api.UploadProfilePictureAsync(image, ct);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Your profile picture has been updated." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RedisplayAsync(ProfileViewModel model, CancellationToken ct)
    {
        var current = await api.GetProfileAsync(ct);
        model.ProfilePictureUrl = current.Data?.ProfilePictureUrl;
        return View(model);
    }
}

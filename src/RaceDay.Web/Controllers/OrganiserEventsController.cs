using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Events management for Organisers: list, create, edit and delete their own events, with a banner upload.
/// Ownership is enforced by the API (403 for someone else's event); the MVC side just shows the right message.
/// </summary>
[RequireRole(RaceDayValues.Organiser)]
[Route("organiser/events")]
public class OrganiserEventsController(IRaceDayApi api) : OrganiserControllerBase(api)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await api.GetEventsAsync(null, null, null, mine: true, ct);
        if (!result.Success) ViewData["Error"] = result.Error;
        return View((result.Data ?? []).OrderByDescending(e => e.Date >= DateTime.Today).ThenBy(e => e.Date).ToList());
    }

    [HttpGet("create")]
    public IActionResult Create() =>
        View("Form", new EventFormViewModel { Date = DateTime.Today.AddDays(30).AddHours(6) });

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken ct)
    {
        model.EventId = null;
        if (!ModelState.IsValid) return View("Form", model);

        var created = await api.CreateEventAsync(model.ToRequest(), ct);
        if (!created.Success || created.Data is null)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "The event could not be created.");
            return View("Form", model);
        }

        var eventId = created.Data.EventId;
        if (model.Banner is { Length: > 0 })
        {
            // The banner is uploaded after the event exists, because the API stores it against the event ID.
            var upload = await api.UploadBannerAsync(eventId, model.Banner, ct);
            if (!upload.Success)
            {
                TempData["Warning"] = $"The event was created, but the banner was not saved: {upload.Error}";
                return RedirectToAction(nameof(Edit), new { id = eventId });
            }
        }

        TempData["Success"] = $"{created.Data.Name} has been created. Now add some categories so participants can enter.";
        return RedirectToAction("Index", "Categories", new { eventId });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var detail = await LoadOwnEventAsync(id, ct);
        if (detail.Result is not null) return detail.Result;
        return View("Form", EventFormViewModel.From(detail.Event!));
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, CancellationToken ct)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View("Form", model);

        // Upload a new banner first, so the update below saves its URL along with the other fields.
        if (model.Banner is { Length: > 0 })
        {
            var upload = await api.UploadBannerAsync(id, model.Banner, ct);
            if (!upload.Success)
            {
                ModelState.AddModelError(nameof(model.Banner), upload.Error ?? "The banner could not be uploaded.");
                return View("Form", model);
            }
            model.BannerImageUrl = upload.Data!.ImageUrl;
        }

        var updated = await api.UpdateEventAsync(id, model.ToRequest(), ct);
        if (!updated.Success)
        {
            if (updated.IsForbidden) return RedirectToAction("AccessDenied", "Account");
            ModelState.AddModelError(string.Empty, updated.Error ?? "The event could not be saved.");
            return View("Form", model);
        }

        TempData["Success"] = $"{model.Name} has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var detail = await LoadOwnEventAsync(id, ct);
        if (detail.Result is not null) return detail.Result;

        var enrolments = await api.GetEventEnrolmentsAsync(id, ct);
        ViewData["EnrolmentCount"] = enrolments.Data?.Count ?? 0;
        return View(detail.Event);
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var result = await api.DeleteEventAsync(id, ct);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "The event has been deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}

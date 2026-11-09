using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Public event detail page, and the Participant's "Enter event" action.</summary>
public class EventsController(IRaceDayApi api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await api.GetEventAsync(id, ct);
        if (result.IsNotFound) return NotFound();
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }

        Enrolment? myEnrolment = null;
        var isOwner = false;
        var session = HttpContext.Session;

        if (session.IsParticipant())
        {
            // So the page can say "You're entered" instead of showing the form again.
            var mine = await api.GetMyEnrolmentsAsync(ct);
            myEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id && e.Status != "Cancelled")
                          ?? mine.Data?.FirstOrDefault(e => e.EventId == id);
        }
        else if (session.IsOrganiser())
        {
            // The owner gets shortcuts to manage the event. The API still checks ownership on every change.
            var mine = await api.GetEventsAsync(null, null, null, mine: true, ct);
            isOwner = mine.Data?.Any(e => e.EventId == id) == true;
        }

        return View(new EventDetailsViewModel { Event = result.Data, MyEnrolment = myEnrolment, IsOwner = isOwner });
    }

    /// <summary>Enters the logged-in Participant into the event in the chosen category. The API records it immediately.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole(RaceDayValues.Participant)]
    public async Task<IActionResult> Enrol(int id, int? categoryId, CancellationToken ct)
    {
        if (categoryId is null)
        {
            TempData["Error"] = "Choose a category before entering the event.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await api.EnrolAsync(id, categoryId.Value, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = $"You're in! You entered {result.Data!.EventName} in the {result.Data.CategoryName} category. Status: {result.Data.Status}.";
        return RedirectToAction("MyEvents", "Participant");
    }
}

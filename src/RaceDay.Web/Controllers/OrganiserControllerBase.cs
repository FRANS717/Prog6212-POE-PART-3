using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helper for the Organiser pages that work on one event (edit, categories, enrolments, results).</summary>
[RequireRole(RaceDayValues.Organiser)]
public abstract class OrganiserControllerBase(IRaceDayApi api) : Controller
{
    /// <summary>
    /// Loads an event and checks it belongs to this Organiser, so management pages are not shown for other
    /// organisers' events. The API enforces the same rule on every change, so this is for a clear message.
    /// </summary>
    protected async Task<(EventDetail? Event, IActionResult? Result)> LoadOwnEventAsync(int id, CancellationToken ct)
    {
        var detail = await api.GetEventAsync(id, ct);
        if (detail.IsNotFound) return (null, NotFound());
        if (!detail.Success || detail.Data is null)
        {
            TempData["Error"] = detail.Error;
            return (null, RedirectToAction("Index", "OrganiserEvents"));
        }

        var mine = await api.GetEventsAsync(null, null, null, mine: true, ct);
        if (mine.Data?.Any(e => e.EventId == id) != true) return (null, RedirectToAction("AccessDenied", "Account"));

        return (detail.Data, null);
    }
}

using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The Organiser's landing page after login.</summary>
[RequireRole(RaceDayValues.Organiser)]
public class OrganiserController(IRaceDayApi api) : Controller
{
    /// <summary>Summary of the Organiser's events, enrolments per event and upcoming dates.</summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var model = new DashboardViewModel();

        // Two API calls: my events (GET /api/events?mine=true) and every enrolment for my events (GET /api/enrolments).
        var events = await api.GetEventsAsync(null, null, null, mine: true, ct);
        var enrolments = await api.GetMyEnrolmentsAsync(ct);

        if (!events.Success)
        {
            model.Error = events.Error;
            return View(model);
        }
        if (!enrolments.Success) model.Error = enrolments.Error;

        var byEvent = (enrolments.Data ?? []).GroupBy(e => e.EventId).ToDictionary(g => g.Key, g => g.ToList());

        model.Events = (events.Data ?? [])
            .Select(e =>
            {
                var list = byEvent.GetValueOrDefault(e.EventId) ?? [];
                return new DashboardEventRow(e,
                    list.Count(x => x.Status != "Cancelled"),
                    list.Count(x => x.Status == "Confirmed"),
                    list.Count(x => x.Status == "Pending"));
            })
            // Upcoming events first (soonest at the top), then past events (most recent first).
            .OrderBy(r => r.Event.Date.Date < DateTime.Today)
            .ThenBy(r => r.Event.Date.Date < DateTime.Today ? -r.Event.Date.Ticks : r.Event.Date.Ticks)
            .ToList();

        return View(model);
    }
}

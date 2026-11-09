using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The Participant's own pages. Organisers and guests cannot open them.</summary>
[RequireRole(RaceDayValues.Participant)]
public class ParticipantController(IRaceDayApi api) : Controller
{
    /// <summary>Every event the Participant has entered, with the category and enrolment status.</summary>
    [HttpGet]
    public async Task<IActionResult> MyEvents(CancellationToken ct)
    {
        var result = await api.GetMyEnrolmentsAsync(ct);
        if (!result.Success) ViewData["Error"] = result.Error;

        var enrolments = (result.Data ?? []).OrderBy(e => e.EventDate < DateTime.Today).ThenBy(e => e.EventDate).ToList();
        return View(enrolments);
    }

    /// <summary>Race history: event, date, category, finish time and "47th out of 312 finishers" for each result.</summary>
    [HttpGet]
    public async Task<IActionResult> MyResults(CancellationToken ct)
    {
        var result = await api.GetMyResultsAsync(ct);
        if (!result.Success) ViewData["Error"] = result.Error;

        // The API already returns the newest race first, so I show the list as it comes.
        return View(result.Data ?? []);
    }
}

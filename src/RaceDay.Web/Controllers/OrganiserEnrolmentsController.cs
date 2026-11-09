using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Every Participant entered in one of the Organiser's events, with their category and status.</summary>
[Route("organiser/events/{eventId:int}/enrolments")]
public class OrganiserEnrolmentsController(IRaceDayApi api) : OrganiserControllerBase(api)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId, string? status, CancellationToken ct)
    {
        var own = await LoadOwnEventAsync(eventId, ct);
        if (own.Result is not null) return own.Result;

        var result = await api.GetEventEnrolmentsAsync(eventId, ct);
        if (!result.Success) ViewData["Error"] = result.Error;

        var list = result.Data ?? [];
        ViewData["Counts"] = RaceDayValues.EnrolmentStatuses.ToDictionary(s => s, s => list.Count(e => e.Status == s));
        if (!string.IsNullOrEmpty(status)) list = list.Where(e => e.Status == status).ToList();

        return View(new EventEnrolmentsViewModel
        {
            Event = own.Event!,
            Enrolments = list.OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName).ToList(),
            StatusFilter = status
        });
    }

    /// <summary>Confirms, cancels or resets an enrolment to Pending.</summary>
    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int eventId, int id, string status, string? filter, CancellationToken ct)
    {
        if (!RaceDayValues.EnrolmentStatuses.Contains(status))
        {
            TempData["Error"] = "Choose Pending, Confirmed or Cancelled.";
        }
        else
        {
            var result = await api.UpdateEnrolmentStatusAsync(id, status, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? $"{result.Data!.ParticipantName} is now {result.Data.Status}."
                : result.Error;
        }

        return RedirectToAction(nameof(Index), new { eventId, status = filter });
    }
}

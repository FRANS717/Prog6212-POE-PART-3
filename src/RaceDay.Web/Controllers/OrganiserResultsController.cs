using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Results entry: capture and publish finish times and positions for enrolled Participants after the event.</summary>
[Route("organiser/events/{eventId:int}/results")]
public class OrganiserResultsController(IRaceDayApi api) : OrganiserControllerBase(api)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        var own = await LoadOwnEventAsync(eventId, ct);
        if (own.Result is not null) return own.Result;

        var enrolments = await api.GetEventEnrolmentsAsync(eventId, ct);
        var results = await api.GetEventResultsAsync(eventId, ct);
        if (!enrolments.Success) ViewData["Error"] = enrolments.Error;
        else if (!results.Success) ViewData["Error"] = results.Error;

        var resultsByEnrolment = (results.Data ?? []).ToDictionary(r => r.EnrolmentId);

        return View(new ResultsEntryViewModel
        {
            Event = own.Event!,
            // Cancelled entries did not race, unless a result was already captured for them.
            Enrolments = (enrolments.Data ?? [])
                .Where(e => e.Status != "Cancelled" || resultsByEnrolment.ContainsKey(e.EnrolmentId))
                .OrderBy(e => resultsByEnrolment.TryGetValue(e.EnrolmentId, out var r) ? r.FinishPosition : int.MaxValue)
                .ThenBy(e => e.ParticipantName)
                .ToList(),
            ResultsByEnrolment = resultsByEnrolment
        });
    }

    /// <summary>Saves one row: creates the result if there is none yet, otherwise corrects it.</summary>
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int eventId, ResultFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (!TimeSpan.TryParseExact(model.FinishTime, [@"h\:mm\:ss", @"hh\:mm\:ss"], CultureInfo.InvariantCulture, out var time) || time <= TimeSpan.Zero)
        {
            TempData["Error"] = "Enter the finish time as h:mm:ss, for example 1:45:30.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        ApiResult<RaceResult> result = model.ResultId is { } resultId
            ? await api.UpdateResultAsync(resultId, new UpdateResultRequest(time, model.FinishPosition!.Value), ct)
            : await api.CaptureResultAsync(eventId, new CaptureResultRequest(model.EnrolmentId, time, model.FinishPosition!.Value), ct);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? $"Result published for {result.Data!.ParticipantName}: {Helpers.StatusStyles.FinishTime(result.Data.FinishTime)}, {Helpers.StatusStyles.Ordinal(result.Data.FinishPosition)}."
            : result.Error;

        return RedirectToAction(nameof(Index), new { eventId });
    }
}

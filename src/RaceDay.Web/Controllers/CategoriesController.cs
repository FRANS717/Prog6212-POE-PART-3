using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Category management for one event: add, edit and remove age or distance categories (e.g. Under 20, 21km).</summary>
[Route("organiser/events/{eventId:int}/categories")]
public class CategoriesController(IRaceDayApi api) : OrganiserControllerBase(api)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        var own = await LoadOwnEventAsync(eventId, ct);
        if (own.Result is not null) return own.Result;
        return View(new CategoriesPageViewModel { Event = own.Event! });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, [Bind(Prefix = "NewCategory")] CategoryFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var own = await LoadOwnEventAsync(eventId, ct);
            if (own.Result is not null) return own.Result;
            return View("Index", new CategoriesPageViewModel { Event = own.Event!, NewCategory = model });
        }

        var result = await api.CreateCategoryAsync(eventId, model.ToRequest(), ct);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? $"Category \"{result.Data!.Name}\" added." : result.Error;
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var result = await api.UpdateCategoryAsync(id, model.ToRequest(), ct);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? $"Category \"{result.Data!.Name}\" updated." : result.Error;
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id, CancellationToken ct)
    {
        // The API refuses (409) to delete a category that participants have entered, and says so.
        var result = await api.DeleteCategoryAsync(id, ct);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Category removed." : result.Error;
        return RedirectToAction(nameof(Index), new { eventId });
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The public home page: upcoming events with filters. Open to guests, Participants and Organisers.</summary>
public class HomeController(IRaceDayApi api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilterViewModel filter, CancellationToken ct)
    {
        var model = new HomeIndexViewModel { Filter = filter };

        // Type, date and location are filtered by the API. I only hide past events here unless asked to show them.
        var result = await api.GetEventsAsync(filter.Type, filter.Date, filter.Location, mine: false, ct);
        if (!result.Success)
        {
            model.Error = result.Error;
            return View(model);
        }

        var today = DateTime.Today;
        model.Events = (result.Data ?? [])
            .Where(e => filter.IncludePast || e.Date.Date >= today)
            .OrderBy(e => e.Date)
            .ToList();

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    /// <summary>Friendly page for 404s and other status codes, instead of the browser's default error.</summary>
    [Route("Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }
}

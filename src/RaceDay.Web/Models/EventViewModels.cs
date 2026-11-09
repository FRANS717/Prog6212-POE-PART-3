using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

/// <summary>The filter bar on the home page. Every value is optional and is sent to GET /api/events.</summary>
public class EventFilterViewModel
{
    [Display(Name = "Event type")]
    public string? Type { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "On date")]
    public DateOnly? Date { get; set; }

    [Display(Name = "Include past events")]
    public bool IncludePast { get; set; }

    public bool HasAny => !string.IsNullOrWhiteSpace(Type) || !string.IsNullOrWhiteSpace(Location) || Date is not null || IncludePast;
}

public class HomeIndexViewModel
{
    public EventFilterViewModel Filter { get; set; } = new();
    public List<EventSummary> Events { get; set; } = [];
    public string? Error { get; set; }
}

/// <summary>The event detail page, with what the current user can do on it.</summary>
public class EventDetailsViewModel
{
    public required EventDetail Event { get; init; }

    /// <summary>The Participant's existing enrolment for this event, if any.</summary>
    public Enrolment? MyEnrolment { get; init; }

    public bool IsPast => Event.Date.Date < DateTime.Today;
    public bool IsOwner { get; init; }

    [Display(Name = "Category")]
    public int? SelectedCategoryId { get; init; }
}

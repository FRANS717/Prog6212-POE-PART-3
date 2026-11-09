using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

/// <summary>One row on the Organiser dashboard: an event and how many people have entered it.</summary>
public record DashboardEventRow(EventSummary Event, int Enrolments, int Confirmed, int Pending);

public class DashboardViewModel
{
    public List<DashboardEventRow> Events { get; set; } = [];
    public string? Error { get; set; }

    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.Event.Date.Date >= DateTime.Today);
    public int TotalEnrolments => Events.Sum(e => e.Enrolments);
    public int PendingEnrolments => Events.Sum(e => e.Pending);
    public DashboardEventRow? NextEvent => Events.Where(e => e.Event.Date.Date >= DateTime.Today).MinBy(e => e.Event.Date);
}

/// <summary>Create and edit form for an event. Every field the brief asks for, plus an optional banner image.</summary>
public class EventFormViewModel
{
    public int? EventId { get; set; }
    public string? BannerImageUrl { get; set; }

    [Required(ErrorMessage = "Enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Describe the event.")]
    [StringLength(2000)]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Choose the date and start time.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Date and start time")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Enter the location.")]
    [StringLength(200)]
    public string Location { get; set; } = "";

    [Required(ErrorMessage = "Enter the distance.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public bool IsEdit => EventId is not null;

    public EventRequest ToRequest() =>
        new(Name.Trim(), Description.Trim(), Date!.Value, Location.Trim(), Distance!.Value, EventType, BannerImageUrl);

    public static EventFormViewModel From(EventDetail e) => new()
    {
        EventId = e.EventId,
        BannerImageUrl = e.BannerImageUrl,
        Name = e.Name,
        Description = e.Description,
        Date = e.Date,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType
    };
}

/// <summary>Add or edit one category on the categories page.</summary>
public class CategoryFormViewModel : IValidatableObject
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Enter the category name.")]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = "";

    [StringLength(255)]
    public string? Description { get; set; }

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    public CategoryRequest ToRequest() =>
        new(Name.Trim(), string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(), MinAge, MaxAge);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinAge is not null && MaxAge is not null && MinAge > MaxAge)
            yield return new ValidationResult("Minimum age cannot be more than maximum age.", [nameof(MinAge)]);
    }
}

public class CategoriesPageViewModel
{
    public required EventDetail Event { get; init; }
    public CategoryFormViewModel NewCategory { get; init; } = new();
}

public class EventEnrolmentsViewModel
{
    public required EventDetail Event { get; init; }
    public List<Enrolment> Enrolments { get; init; } = [];
    public string? StatusFilter { get; init; }
}

/// <summary>The results entry page: every enrolment, with its result if one has been captured.</summary>
public class ResultsEntryViewModel
{
    public required EventDetail Event { get; init; }
    public List<Enrolment> Enrolments { get; init; } = [];
    public Dictionary<int, RaceResult> ResultsByEnrolment { get; init; } = [];

    public bool EventHasHappened => Event.Date <= DateTime.Now;
}

/// <summary>A result posted from the results entry page. FinishTime is typed as h:mm:ss.</summary>
public class ResultFormViewModel
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    [Required(ErrorMessage = "Enter the finish time.")]
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use h:mm:ss, for example 1:45:30.")]
    public string FinishTime { get; set; } = "";

    [Required(ErrorMessage = "Enter the finishing position.")]
    [Range(1, 100000, ErrorMessage = "Position must be 1 or more.")]
    public int? FinishPosition { get; set; }
}

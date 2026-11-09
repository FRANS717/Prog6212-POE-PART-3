namespace RaceDay.Web.Helpers;

/// <summary>
/// One place that decides the colour of every status badge, so the colours are the same on every page:
/// green for Confirmed / Published, amber for Pending, red for Cancelled / Disqualified.
/// </summary>
public static class StatusStyles
{
    public static string BadgeClass(string? status) => status switch
    {
        "Confirmed" or "Published" => "rd-status rd-status-confirmed",
        "Pending" => "rd-status rd-status-pending",
        "Cancelled" or "Disqualified" => "rd-status rd-status-cancelled",
        _ => "rd-status"
    };

    public static string Icon(string? status) => status switch
    {
        "Confirmed" or "Published" => "bi-check-circle-fill",
        "Pending" => "bi-hourglass-split",
        "Cancelled" or "Disqualified" => "bi-x-circle-fill",
        _ => "bi-circle"
    };

    public static string EventTypeIcon(string? eventType) => eventType switch
    {
        "Cycle" => "bi-bicycle",
        "Walk" => "bi-person-walking",
        _ => "bi-lightning-charge-fill"
    };

    /// <summary>1 becomes 1st, 2 becomes 2nd, 11 becomes 11th, 23 becomes 23rd. Used for "47th out of 312 finishers".</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13) return $"{number}th";
        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    /// <summary>Formats a finish time as h:mm:ss, for example 3:45:30.</summary>
    public static string FinishTime(TimeSpan time) =>
        $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
}

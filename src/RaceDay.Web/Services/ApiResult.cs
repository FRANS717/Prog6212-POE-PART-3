using System.Net;

namespace RaceDay.Web.Services;

/// <summary>
/// The outcome of one API call. Controllers check <see cref="Success"/> instead of catching exceptions,
/// and show <see cref="Error"/> to the user, which is always a friendly sentence and never a stack trace.
/// </summary>
public class ApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string? Error { get; init; }

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
}

/// <summary>An API outcome that also carries the response body when the call succeeded.</summary>
public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

using System.Net;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests;

/// <summary>
/// Checks that the MVC app talks to the API the way the brief requires: it keeps Jarred's API session cookie after
/// login, forwards it on every call, and turns API errors into friendly messages instead of crashing.
/// </summary>
public class ApiClientTests
{
    [Fact]
    public async Task Login_keeps_the_API_session_cookie_for_Jarred()
    {
        var handler = new FakeApiHandler(_ =>
        {
            var response = FakeApiHandler.Json(HttpStatusCode.OK, """{"userId":7,"email":"jarred@raceday.com","role":"Participant"}""");
            response.Headers.Add("Set-Cookie", "RaceDay.Session=abc123; path=/; samesite=lax; httponly");
            return response;
        });
        var api = TestFactory.Client(handler, TestFactory.HttpContext());

        var result = await api.LoginAsync(new LoginRequest("jarred@raceday.com", "Password123!"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("abc123", result.ApiCookie);
        Assert.Equal("Participant", result.Data!.Role);
    }

    [Fact]
    public async Task Wrong_password_returns_the_API_message()
    {
        var handler = new FakeApiHandler(_ => FakeApiHandler.Json(HttpStatusCode.Unauthorized, """{"message":"Invalid email or password."}"""));
        var api = TestFactory.Client(handler, TestFactory.HttpContext());

        var result = await api.LoginAsync(new LoginRequest("jarred@raceday.com", "wrong"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Error);
    }

    [Fact]
    public async Task Logged_in_requests_forward_Jarreds_API_session_cookie()
    {
        var session = new TestSession();
        session.SignIn(7, "jarred@raceday.com", "Participant", "Jarred", "abc123");
        var handler = new FakeApiHandler(_ => FakeApiHandler.Json(HttpStatusCode.OK, "[]"));
        var api = TestFactory.Client(handler, TestFactory.HttpContext(session));

        await api.GetMyEnrolmentsAsync(CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/enrolments", request.RequestUri!.AbsolutePath);
        Assert.Equal("RaceDay.Session=abc123", Assert.Single(request.Headers.GetValues("Cookie")));
    }

    [Fact]
    public async Task Guest_requests_send_no_cookie()
    {
        var handler = new FakeApiHandler(_ => FakeApiHandler.Json(HttpStatusCode.OK, "[]"));
        var api = TestFactory.Client(handler, TestFactory.HttpContext());

        await api.GetEventsAsync("Run", new DateOnly(2026, 12, 5), "Cape Town", mine: false, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.False(request.Headers.Contains("Cookie"));
        Assert.Equal("?type=Run&date=2026-12-05&location=Cape%20Town", request.RequestUri!.Query);
    }

    [Fact]
    public async Task Expired_API_session_for_a_logged_in_user_throws_so_they_can_log_in_again()
    {
        var session = new TestSession();
        session.SignIn(7, "jarred@raceday.com", "Organiser", "Jarred", "expired");
        var handler = new FakeApiHandler(_ => FakeApiHandler.Json(HttpStatusCode.Unauthorized, """{"message":"You must be logged in to do this."}"""));
        var api = TestFactory.Client(handler, TestFactory.HttpContext(session));

        await Assert.ThrowsAsync<ApiSessionExpiredException>(() => api.GetEventsAsync(null, null, null, mine: true, CancellationToken.None));
    }

    [Fact]
    public async Task Forbidden_role_gets_a_friendly_error_not_an_exception()
    {
        var session = new TestSession();
        session.SignIn(7, "jarred@raceday.com", "Participant", "Jarred", "abc123");
        var handler = new FakeApiHandler(_ => FakeApiHandler.Json(HttpStatusCode.Forbidden, """{"message":"Your role does not have access to this action."}"""));
        var api = TestFactory.Client(handler, TestFactory.HttpContext(session));

        var result = await api.DeleteEventAsync(1, CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.IsForbidden);
        Assert.Equal("Your role does not have access to this action.", result.Error);
    }

    [Fact]
    public async Task Unreachable_API_returns_a_friendly_message()
    {
        var handler = new FakeApiHandler(_ => throw new HttpRequestException("connection refused"));
        var api = TestFactory.Client(handler, TestFactory.HttpContext());

        var result = await api.GetEventsAsync(null, null, null, mine: false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(RaceDayApiClient.ServiceDownMessage, result.Error);
    }

    [Fact]
    public async Task Error_without_a_json_body_still_gets_a_readable_message()
    {
        var handler = new FakeApiHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var api = TestFactory.Client(handler, TestFactory.HttpContext());

        var result = await api.GetEventAsync(999, CancellationToken.None);

        Assert.True(result.IsNotFound);
        Assert.Equal("That item could not be found.", result.Error);
    }
}

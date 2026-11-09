using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests;

/// <summary>Role-based navigation rules and the display helpers used on Jarred's pages.</summary>
public class NavigationAndDisplayTests
{
    private static AuthorizationFilterContext Run(RequireRoleAttribute filter, TestSession session, string path = "/Participant/MyEvents")
    {
        var http = TestFactory.HttpContext(session);
        http.Request.Path = path;
        var context = new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), []);
        filter.OnAuthorization(context);
        return context;
    }

    [Fact]
    public void Guest_is_sent_to_login_with_a_return_url()
    {
        var context = Run(new RequireRoleAttribute(RaceDayValues.Participant), new TestSession());

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("/Participant/MyEvents", redirect.RouteValues!["returnUrl"]?.ToString());
    }

    [Fact]
    public void Organiser_Jarred_cannot_open_participant_pages()
    {
        var session = new TestSession();
        session.SignIn(1, "jarred@raceday.com", RaceDayValues.Organiser, "Jarred", "c");

        var context = Run(new RequireRoleAttribute(RaceDayValues.Participant), session);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("AccessDenied", redirect.ActionName);
    }

    [Fact]
    public void Participant_Jarred_can_open_participant_pages()
    {
        var session = new TestSession();
        session.SignIn(2, "jarred@raceday.com", RaceDayValues.Participant, "Jarred", "c");

        var context = Run(new RequireRoleAttribute(RaceDayValues.Participant), session);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("Confirmed", "rd-status-confirmed")]
    [InlineData("Pending", "rd-status-pending")]
    [InlineData("Cancelled", "rd-status-cancelled")]
    [InlineData("Published", "rd-status-confirmed")]
    public void Statuses_are_colour_coded(string status, string expectedClass)
    {
        Assert.Contains(expectedClass, StatusStyles.BadgeClass(status));
    }

    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(11, "11th")]
    [InlineData(22, "22nd")]
    [InlineData(47, "47th")]
    [InlineData(113, "113th")]
    public void Positions_read_like_a_results_sheet(int position, string expected)
    {
        Assert.Equal(expected, StatusStyles.Ordinal(position));
    }

    [Fact]
    public void Finish_time_is_shown_as_hours_minutes_seconds()
    {
        Assert.Equal("3:45:30", StatusStyles.FinishTime(new TimeSpan(3, 45, 30)));
        Assert.Equal("0:42:05", StatusStyles.FinishTime(new TimeSpan(0, 42, 5)));
    }

    [Fact]
    public void Blob_urls_are_used_as_is_and_local_urls_point_at_the_api()
    {
        var images = new ImageUrls(Options.Create(new ApiOptions { BaseUrl = "http://host.docker.internal:5043", PublicBaseUrl = "http://localhost:5043" }));

        Assert.Equal("https://acct.blob.core.windows.net/raceday-images/banners/a.png",
            images.For("https://acct.blob.core.windows.net/raceday-images/banners/a.png"));
        Assert.Equal("http://localhost:5043/uploads/banners/a.png", images.For("/uploads/banners/a.png"));
        Assert.Null(images.For(null));
    }
}

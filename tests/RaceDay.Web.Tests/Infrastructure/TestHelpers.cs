using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.Web.Services;

namespace RaceDay.Web.Tests.Infrastructure;

/// <summary>Fake HTTP handler: records each request the MVC client sends and replies with a canned response.</summary>
public class FakeApiHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        return Task.FromResult(respond(request));
    }

    public static HttpResponseMessage Json(HttpStatusCode code, string json) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

/// <summary>A minimal in-memory ISession so tests can log Jarred in without a web server.</summary>
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = [];
    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;
    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}

public static class TestFactory
{
    public static DefaultHttpContext HttpContext(TestSession? session = null)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<Microsoft.AspNetCore.Http.Features.ISessionFeature>(new SessionFeature { Session = session ?? new TestSession() });
        return context;
    }

    public static RaceDayApiClient Client(FakeApiHandler handler, HttpContext context) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") },
            new HttpContextAccessor { HttpContext = context },
            NullLogger<RaceDayApiClient>.Instance);

    private class SessionFeature : Microsoft.AspNetCore.Http.Features.ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }
}

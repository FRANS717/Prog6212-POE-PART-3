using Microsoft.Extensions.Options;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ApiSessionExpiredFilter turns an expired API session into a friendly "please log in again" redirect.
builder.Services.AddControllersWithViews(options => options.Filters.Add<ApiSessionExpiredFilter>());

// Where the Part 2 API is. Override with the environment variable Api__BaseUrl (for example inside Docker).
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.Section));
builder.Services.AddSingleton<ImageUrls>();
builder.Services.AddHttpContextAccessor();

// All data comes from the API through this typed client. UseCookies = false means the handler does not keep
// one shared cookie jar for every user: I attach each user's own API session cookie per request instead.
builder.Services.AddHttpClient<IRaceDayApi, RaceDayApiClient>((services, client) =>
    {
        var options = services.GetRequiredService<IOptions<ApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

// The MVC app's own server-side session keeps the user ID, role and API session cookie across pages.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// Users never see a stack trace or a raw error, in any environment: unexpected exceptions go to a friendly
// error page, and 404s and other status codes get a branded page. Details are still written to the log.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// One fixed culture so "21.1" km and dates bind the same way on every machine and inside the Linux container.
// (A South African Windows culture uses a comma as the decimal separator, which would break number inputs.)
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-GB")
    .AddSupportedCultures("en-GB")
    .AddSupportedUICultures("en-GB"));

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

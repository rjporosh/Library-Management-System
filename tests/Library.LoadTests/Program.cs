using System.Net.Http.Headers;
using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// Load / stress scenarios for the Library API.
//   dotnet run --project tests/Library.LoadTests -c Release [-- <baseUrl>]
// Requires a running API (e.g. `docker compose up` or `dotnet run --project src/Library.Api`)
// seeded with the default demo accounts (see docs/ai-handover.md).

var baseUrl = (args.Length > 0 ? args[0] : "http://localhost:5254").TrimEnd('/');
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

// Every scenario below hits an endpoint that now requires a bearer token
// (auth was added after these scenarios were first written) - log in as
// the seeded librarian once, up front, and reuse the token for every request.
var loginResponse = await http.PostAsJsonAsync("/api/auth/login", new
{
    usernameOrEmail = "librarian",
    password = "Librarian@123",
});
loginResponse.EnsureSuccessStatusCode();
var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

var dashboard = Scenario.Create("dashboard", async _ =>
{
    var request = Http.CreateRequest("GET", "/api/dashboard");
    var response = await Http.Send(http, request);
    return response;
})
.WithLoadSimulations(Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)));

var search = Scenario.Create("books-search", async _ =>
{
    var request = Http.CreateRequest("POST", "/api/books/search")
        .WithJsonBody(new
        {
            filters = new[] { new { field = "author", op = "contains", value = "martin" } },
            sort = new[] { new { field = "publishedYear", direction = "desc" } },
            page = 1,
            pageSize = 20,
        });
    var response = await Http.Send(http, request);
    return response;
})
.WithLoadSimulations(Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)));

var issueReturn = Scenario.Create("member-search", async _ =>
{
    var request = Http.CreateRequest("POST", "/api/members/search")
        .WithJsonBody(new { filters = Array.Empty<object>(), page = 1, pageSize = 20 });
    var response = await Http.Send(http, request);
    return response;
})
.WithLoadSimulations(Simulation.Inject(rate: 15, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)));

NBomberRunner
    .RegisterScenarios(dashboard, search, issueReturn)
    .WithReportFolder("reports")
    .Run();

internal sealed record LoginResult(string AccessToken);

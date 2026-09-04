using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// Load / stress scenarios for the Library API.
//   dotnet run --project tests/Library.LoadTests -c Release [-- <baseUrl>]
// Requires a running API (e.g. `docker compose up` or `dotnet run --project src/Library.Api`).

var baseUrl = (args.Length > 0 ? args[0] : "http://localhost:5254").TrimEnd('/');
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

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

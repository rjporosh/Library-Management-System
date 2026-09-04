using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Library.IntegrationTests.Common;

/// <summary>
/// Boots the API with the in-memory provider so the HTTP integration tests run
/// without a database. EF-specific behaviour is covered separately with Sqlite.
/// </summary>
public sealed class LibraryApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Database:MigrateOnStartup"] = "false",
            });
        });
    }
}

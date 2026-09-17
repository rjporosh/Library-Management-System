using System.Net.Http.Headers;
using Library.Application.Common.Options;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Security;
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
    private const string SigningKey = "integration-test-signing-key-not-for-production-0123456789";

    private static readonly JwtOptions TestJwtOptions = new()
    {
        Issuer = "LibraryManagementSystem",
        Audience = "LibraryManagementSystem.Client",
        SigningKey = SigningKey,
        AccessTokenMinutes = 60,
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Jwt:Issuer", TestJwtOptions.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwtOptions.Audience);
        builder.UseSetting("Jwt:SigningKey", TestJwtOptions.SigningKey);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Database:MigrateOnStartup"] = "false",
                ["Jwt:Issuer"] = TestJwtOptions.Issuer,
                ["Jwt:Audience"] = TestJwtOptions.Audience,
                ["Jwt:SigningKey"] = TestJwtOptions.SigningKey,
            });
        });
    }

    /// <summary>An <see cref="HttpClient"/> pre-authenticated as a Librarian - the default for existing staff-workflow tests.</summary>
    public HttpClient CreateLibrarianClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateLibrarianToken());
        return client;
    }

    /// <summary>An <see cref="HttpClient"/> pre-authenticated as a Member linked to <paramref name="memberId"/>.</summary>
    public HttpClient CreateMemberClient(Guid memberId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateMemberToken(memberId));
        return client;
    }

    /// <summary>A bearer token for a Librarian, for tests exercising staff-only endpoints.</summary>
    public static string CreateLibrarianToken()
    {
        var user = new User(Guid.NewGuid(), "test-librarian", "test-librarian@library.local", "unused", UserRole.Librarian);
        return new JwtTokenService(TestJwtOptions).CreateAccessToken(user).Token;
    }

    /// <summary>A bearer token for a Member account linked to <paramref name="memberId"/>.</summary>
    public static string CreateMemberToken(Guid memberId)
    {
        var user = new User(Guid.NewGuid(), "test-member", "test-member@library.local", "unused", UserRole.Member, memberId);
        return new JwtTokenService(TestJwtOptions).CreateAccessToken(user).Token;
    }
}

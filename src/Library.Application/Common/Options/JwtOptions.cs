namespace Library.Application.Common.Options;

/// <summary>
/// Bound from the "Jwt" section of appsettings. <see cref="SigningKey"/> must
/// be overridden per environment (env var <c>Jwt__SigningKey</c> in
/// production) - the value shipped in appsettings.json is a development-only
/// placeholder, never a real secret.
/// </summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "LibraryManagementSystem";

    public string Audience { get; set; } = "LibraryManagementSystem.Client";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 120;
}

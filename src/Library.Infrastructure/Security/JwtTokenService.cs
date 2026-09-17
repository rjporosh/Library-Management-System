using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Library.Application.Abstractions;
using Library.Application.Common.Options;
using Library.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Library.Infrastructure.Security;

public sealed class JwtTokenService(JwtOptions options) : ITokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Set it in appsettings (Development) " +
                "or the Jwt__SigningKey environment variable (Production).");
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.MemberId is { } memberId)
        {
            claims.Add(new Claim("memberId", memberId.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}

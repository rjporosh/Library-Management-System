using Library.Domain.Entities;

namespace Library.Application.Abstractions;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user);
}

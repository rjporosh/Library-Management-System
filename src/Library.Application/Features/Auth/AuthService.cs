using Library.Application.Abstractions;
using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Results;
using Library.Application.Common.Security;
using Library.Application.Features.Auth.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IMemberRepository memberRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var invalidCredentials = Result.Failure<LoginResponse>(
            new ApiError(ErrorCodes.AuthInvalidCredentials, "Username/email or password is incorrect.", "usernameOrEmail"));

        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return invalidCredentials;
        }

        var user = await userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail.Trim(), cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return invalidCredentials;
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(
                new ApiError(ErrorCodes.AuthAccountInactive, "This account has been deactivated.", "usernameOrEmail"));
        }

        user.RecordLogin();
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAtUtc) = tokenService.CreateAccessToken(user);
        return Result.Success(new LoginResponse(token, expiresAtUtc, user.Id, user.Username, user.Role, user.MemberId));
    }

    /// <summary>Self-service sign-up: creates the Member profile and its login account together.</summary>
    public async Task<Result<LoginResponse>> RegisterMemberAsync(RegisterMemberRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ApiError>();
        FieldRulesForAuth.Required(request.Name, "name", ErrorCodes.MemberNameRequired, "Name is required.", errors);
        FieldRulesForAuth.Email(request.Email, "email", errors);
        FieldRulesForAuth.Password(request.Password, "password", errors);

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            await memberRepository.ExistsByEmailAsync(request.Email.Trim(), null, cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.MemberEmailDuplicate, "A member with this email already exists.", "email"));
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            await userRepository.ExistsByEmailAsync(request.Email.Trim(), cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.AuthEmailDuplicate, "An account with this email already exists.", "email"));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<LoginResponse>(errors);
        }

        var membershipNumber = $"MEM-{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(10, 99)}";
        var member = new Member(
            Guid.NewGuid(), membershipNumber, request.Name.Trim(), request.Email.Trim(),
            membershipExpiresAt: null, phone: request.Phone.Trim(), address: request.Address.Trim());
        await memberRepository.AddAsync(member, cancellationToken);

        var user = new User(
            Guid.NewGuid(), request.Email.Trim(), request.Email.Trim(),
            passwordHasher.Hash(request.Password), UserRole.Member, member.Id);
        await userRepository.AddAsync(user, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAtUtc) = tokenService.CreateAccessToken(user);
        return Result.Success(new LoginResponse(token, expiresAtUtc, user.Id, user.Username, user.Role, user.MemberId));
    }

    /// <summary>Librarian-only: provisions another staff account.</summary>
    public async Task<Result<LoginResponse>> RegisterLibrarianAsync(RegisterLibrarianRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ApiError>();
        FieldRulesForAuth.Required(request.Username, "username", ErrorCodes.AuthUsernameRequired, "Username is required.", errors);
        FieldRulesForAuth.Email(request.Email, "email", errors);
        FieldRulesForAuth.Password(request.Password, "password", errors);

        if (!string.IsNullOrWhiteSpace(request.Username) &&
            await userRepository.ExistsByUsernameAsync(request.Username.Trim(), cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.AuthUsernameDuplicate, "This username is already taken.", "username"));
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            await userRepository.ExistsByEmailAsync(request.Email.Trim(), cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.AuthEmailDuplicate, "An account with this email already exists.", "email"));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<LoginResponse>(errors);
        }

        var user = new User(
            Guid.NewGuid(), request.Username.Trim(), request.Email.Trim(),
            passwordHasher.Hash(request.Password), UserRole.Librarian);
        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAtUtc) = tokenService.CreateAccessToken(user);
        return Result.Success(new LoginResponse(token, expiresAtUtc, user.Id, user.Username, user.Role, user.MemberId));
    }
}

/// <summary>Field rules specific to auth (username / password) not covered by the shared <c>FieldRules</c> set.</summary>
internal static class FieldRulesForAuth
{
    public static void Required(string? value, string field, string code, string message, List<ApiError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApiError(code, message, field, Required: true));
        }
    }

    public static void Email(string? value, string field, List<ApiError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApiError(ErrorCodes.MemberEmailRequired, "Email is required.", field, Required: true));
            return;
        }

        if (!value.Contains('@') || !value.Contains('.'))
        {
            errors.Add(new ApiError(ErrorCodes.MemberEmailInvalid, "Email is not a valid address.", field, SupportedValues: SupportedValues.Email));
        }
    }

    public static void Password(string? value, string field, List<ApiError> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
        {
            errors.Add(new ApiError(
                ErrorCodes.AuthPasswordTooWeak,
                "Password must be at least 8 characters long.",
                field,
                Required: true,
                SupportedValues: "AT LEAST 8 CHARACTERS"));
        }
    }
}

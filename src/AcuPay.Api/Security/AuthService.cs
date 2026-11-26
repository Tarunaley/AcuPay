using AcuPay.Api.Dtos;
using AcuPay.Domain;
using AcuPay.Domain.Abstractions;
using AcuPay.Domain.Exceptions;

namespace AcuPay.Api.Security;

public sealed class AuthService(IUserRepository users, IPasswordHasher hasher, JwtTokenService tokens)
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct)
    {
        var user = await users.GetByUsernameAsync(request.Username.Trim(), ct);

        // Same message for unknown user and wrong password: do not reveal which one failed.
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new AppException(ErrorCodes.InvalidCredentials, "Invalid username or password.", 401);

        var (token, expires) = tokens.Create(user.Username, user.Role);
        return new LoginResponseDto(token, expires, user.Username, user.Role);
    }
}

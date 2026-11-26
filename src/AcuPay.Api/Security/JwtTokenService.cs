using System.Security.Claims;
using System.Text;
using AcuPay.Domain.Settings;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AcuPay.Api.Security;

public sealed class JwtTokenService(JwtSettings jwt, AcuPaySettings settings)
{
    public (string Token, DateTime ExpiresAtUtc) Create(string username, string role)
    {
        var expires = DateTime.UtcNow.AddMinutes(settings.TokenExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", username),
                new Claim("name", username),
                new Claim("role", role)
            }),
            Expires = expires,
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}

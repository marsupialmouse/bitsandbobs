using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BitsAndBobs.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BitsAndBobs.Features.Identity;

public class JwtTokenFactory(IOptions<JwtOptions> options)
{
    /// <summary>
    /// Creates a JWT token for the given user.
    /// </summary>
    public string CreateFor(ClaimsPrincipal claimsPrincipal)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            claims: [claimsPrincipal.Claims.First(x => x.Type == ClaimTypes.NameIdentifier)],
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

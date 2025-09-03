using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BitsAndBobs.Features.Identity;

public static class GetJwtTokenEndpoint
{
    public record GetJwtTokenResponse(string Token);

    /// <summary>
    /// Creates a JWT token for the current user with a dangerously-long expiry for use with the MCP tools.
    /// </summary>
    /// <remarks>You would never do this for real, but setting up OAuth to test MCP doesn't interest me.</remarks>
    public static Task<GetJwtTokenResponse> GetJwtToken(
        ClaimsPrincipal claimsPrincipal,
        [FromServices] JwtTokenFactory tokenFactory
    ) => Task.FromResult(new GetJwtTokenResponse(tokenFactory.CreateFor(claimsPrincipal)));
}

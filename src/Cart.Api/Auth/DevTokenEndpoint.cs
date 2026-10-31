using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cart.Api.Auth;

public sealed record DevTokenRequest(string? UserId);

public sealed record DevTokenResponse(string AccessToken, string UserId, DateTimeOffset ExpiresAt);

public static class DevTokenEndpoint
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public static void MapDevTokenEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/dev/token", (DevTokenRequest? request, JwtOptions options, TimeProvider timeProvider) =>
        {
            var userId = string.IsNullOrWhiteSpace(request?.UserId) ? Guid.NewGuid().ToString() : request.UserId;
            var expiresAt = timeProvider.GetUtcNow().Add(Lifetime);

            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = options.Issuer,
                Audience = options.Audience,
                Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = userId },
                Expires = expiresAt.UtcDateTime,
                SigningCredentials = new SigningCredentials(options.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
            });

            return new DevTokenResponse(token, userId, expiresAt);
        });
}

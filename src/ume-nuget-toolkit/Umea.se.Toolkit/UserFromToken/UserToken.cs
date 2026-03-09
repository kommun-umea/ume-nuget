using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Umea.se.Toolkit.UserFromToken;

/// <summary>
/// Access to the Token of the logged-in user.
/// GetClaimValue provides all claims. Properties provide easy access to typical claims.
/// </summary>
public class UserToken(IHttpContextAccessor httpContextAccessor)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string? SsNo => GetClaimValue("socialSecurityNumber");

    public string? FullName => GetClaimValue("fullName").ToTitleCase();

    public string? Email => GetClaimValue("email");

    public string[] Groups => GetClaimValue("groups")?.Split(',') ?? [];

    public string? GetClaimValue(string claimType)
    {
        ClaimsPrincipal? userPrincipal = _httpContextAccessor.HttpContext?.User;
        string? mappedType = MapToLongClaimType(claimType);
        Claim? claim = userPrincipal?.Claims.FirstOrDefault(w =>
            w.Type == claimType || w.Type == mappedType);

        return string.IsNullOrWhiteSpace(claim?.Value)
            ? null
            : claim.Value;
    }

    private static string? MapToLongClaimType(string claimType) => claimType switch
    {
        "email" => ClaimTypes.Email,
        "name" => ClaimTypes.Name,
        "sub" => ClaimTypes.NameIdentifier,
        "given_name" => ClaimTypes.GivenName,
        "family_name" => ClaimTypes.Surname,
        "role" => ClaimTypes.Role,
        _ => null
    };
}

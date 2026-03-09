using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Umea.se.TestToolkit.Mocks;

/// <summary>
/// This class overrides the actual authorization to always be successful.
/// This also sets the claims of the logged-in user.
/// </summary>
public class LoggedInUserMock(IAuthorizationService authorization) : PolicyEvaluator(authorization)
{
    private string? _ssNo;
    private string? _name;
    private string? _emailAddress;
    private string? _groups;
    private bool overrideAuth = true;

    public LoggedInUserMock WithName(string userFullName)
    {
        _name = userFullName;
        return this;
    }

    public LoggedInUserMock WithSsNo(string userSsNo)
    {
        _ssNo = userSsNo;
        return this;
    }

    public LoggedInUserMock WithEmail(string userEmail)
    {
        _emailAddress = userEmail;
        return this;
    }

    /// <summary>
    /// Implicitly disables AuthOverride, i.e. calls WithActualAuthorization()
    /// </summary>
    public LoggedInUserMock WithAuthGroups(string authorizationGroups)
    {
        _groups = authorizationGroups;
        return WithActualAuthorization();
    }

    public LoggedInUserMock WithActualAuthorization()
    {
        overrideAuth = false;
        return this;
    }

    public override async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy _, HttpContext __)
    {
        List<Claim> claims = [];
        if (_ssNo != null)
        {
            claims.Add(new Claim("socialSecurityNumber", _ssNo));
        }

        if (_name != null)
        {
            claims.Add(new Claim("fullName", _name));
        }

        if (_groups != null)
        {
            claims.Add(new Claim("groups", _groups));
        }

        if (_emailAddress != null)
        {
            claims.Add(new Claim("email", _emailAddress));
        }
        // We can set up further claims here, e.g. Consumer or TemplateAdmin.

        ClaimsPrincipal principal = new();
        principal.AddIdentity(new ClaimsIdentity(
            claims,
            "TestScheme"));

        return await Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal,
            new AuthenticationProperties(), "TestScheme")));
    }

    public override async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy _,
        AuthenticateResult __, HttpContext ___, object? ____)
    {
        if (overrideAuth)
        {
            return await Task.FromResult(PolicyAuthorizationResult.Success());
        }
        else
        {
            return await base.AuthorizeAsync(_, __, ___, ____);
        }
    }
}

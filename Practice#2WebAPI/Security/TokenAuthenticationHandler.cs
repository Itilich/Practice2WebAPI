using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Practice_2WebAPI.Security;

public sealed class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IDataProtector _protector;
    public TokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IDataProtectionProvider provider)
        : base(options, logger, encoder) => _protector = provider.CreateProtector("Practice2.ApiToken.v1");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var value = Request.Headers.Authorization.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());
        try
        {
            var claims = _protector.Unprotect(value[7..]).Split('|');
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, claims[0]), new Claim(ClaimTypes.Role, claims[1]) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
        catch { return Task.FromResult(AuthenticateResult.Fail("Invalid token")); }
    }
}

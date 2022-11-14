using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Yarp.Shared;

namespace Yarp.Service.Infrastructure;

public class PassportAuthenticationHandler : AuthenticationHandler<PassportAuthenticationOptions>
{
    private readonly IPassportService _passportService;

    public PassportAuthenticationHandler(IPassportService passportService, IOptionsMonitor<PassportAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) 
        : base(options, logger, encoder, clock)
    {
        _passportService = passportService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? authorization = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(authorization))
            return AuthenticateResult.NoResult();

        string? token = null;
        if (authorization.StartsWith($"{PassportDefaults.AuthenticationScheme} ", StringComparison.OrdinalIgnoreCase))
            token = authorization.Substring($"{PassportDefaults.AuthenticationScheme} ".Length).Trim();

        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        var bytes = Convert.FromBase64String(token);
        try
        {
            var passport = await _passportService.Read(bytes);
            var identity = new ClaimsIdentity(PassportDefaults.AuthenticationScheme);

            identity.AddClaim(new Claim(ClaimTypes.Name, passport.User.UserName));
            
            return AuthenticateResult.Success(
                new AuthenticationTicket(
                    new ClaimsPrincipal(identity), PassportDefaults.AuthenticationScheme));
        }
        catch (PassportException ex)
        {
            Logger.LogError("{AuthenticationFailure}", ex.Message);
            
            return AuthenticateResult.Fail(ex.Message);
        }
    }
}
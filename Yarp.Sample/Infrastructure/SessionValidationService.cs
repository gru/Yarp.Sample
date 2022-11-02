using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Web;
using IdentityModel;
using Microsoft.Extensions.Caching.Memory;
using Yarp.Sample.Keycloak;

namespace Yarp.Sample.Infrastructure;

public class SessionValidationService : ISessionValidationService
{
    private readonly IMemoryCache _cache;
    private readonly IKeycloakApi _keycloakApi;

    public SessionValidationService(IMemoryCache cache, IKeycloakApi keycloakApi)
    {
        _cache = cache;
        _keycloakApi = keycloakApi;
    }
    
    public Task<bool> Validate(ClaimsPrincipal? principal, out string? error)
    {
        error = null;
        if (principal == null || principal.Identity is not { IsAuthenticated: true })
        {
            error = "User not authorized";
        }
        else
        {
            var session = principal.FindFirst(JwtClaimTypes.SessionId);
            if (session == null)
            {
                error = "Session missing";
            }
            else
            {
                if (_cache.TryGetValue(session.Value, out _))
                {
                    error = "Session revoked";
                }
            }
        }

        return Task.FromResult(error == null);
    }

    public async Task BlockUser(string user)
    {
        var settings = await _keycloakApi.GetRealmSettings();
        var sessions = await _keycloakApi.GetUserSessions(user);
        
        foreach (var session in sessions)
        {
            var sessionLifespan = settings.ClientSessionMax > 0
                ? settings.ClientSessionMax
                : settings.SsoSessionMax;

            _cache.Set(session.Id, session.Id, TimeSpan.FromSeconds(sessionLifespan.Value));
        }

        await _keycloakApi.LogoutUser(user);
    }
}
using System.Security.Claims;

namespace Yarp.Sample.Infrastructure;

public interface ISessionValidationService
{
    Task<bool> Validate(ClaimsPrincipal? principal, out string? error);

    Task BlockUser(string user);
}
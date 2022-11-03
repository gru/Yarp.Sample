using System.Security.Claims;

namespace Yarp.Service.Controllers;

internal static class ClaimsPrincipalEx
{
    public static string GetName(this ClaimsPrincipal principal) => 
        principal.Identity?.Name ?? "Anonymous";
}
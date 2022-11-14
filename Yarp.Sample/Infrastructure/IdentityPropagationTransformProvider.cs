using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using Yarp.Shared;

namespace Yarp.Sample.Infrastructure;

public class IdentityPropagationTransformProvider : ITransformProvider
{
    private readonly IPassportService _passportService;

    public IdentityPropagationTransformProvider(IPassportService passportService)
    {
        _passportService = passportService;
    }
    
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        if ((context.Route.Metadata?.TryGetValue("IdentityPropagation", out var value) ?? false) && bool.TryParse(value, out var exchangeToken) && exchangeToken)
        {
            context.AddRequestTransform(async ctx =>
            {
                if (ctx.HttpContext.User is { Identity: { IsAuthenticated: true, Name: {} } })
                {
                    const string passportScheme = $"Passport";

                    var user = new User(ctx.HttpContext.User.Identity.Name);
                    var bytes = await _passportService.Write(user);
                    var base64 = Convert.ToBase64String(bytes);
                    
                    ctx.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(passportScheme, base64);
                }
            });
        }
    }
}
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Yarp.Sample.Infrastructure;

public class TokenExchangeTransformProvider : ITransformProvider
{
    private readonly ITokenExchangeClient _tokenExchangeClient;

    public TokenExchangeTransformProvider(ITokenExchangeClient tokenExchangeClient)
    {
        _tokenExchangeClient = tokenExchangeClient;
    }
    
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        if ((context.Route.Metadata?.TryGetValue("ExchangeToken", out var value) ?? false) && bool.TryParse(value, out var exchangeToken) && exchangeToken)
        {
            context.AddRequestTransform(async ctx =>
            {
                var authorization = ctx.HttpContext.Request.Headers.Authorization.SingleOrDefault<string>();
                if (authorization != null)
                {
                    const string bearerScheme = $"{JwtBearerDefaults.AuthenticationScheme} ";
                
                    if (authorization.StartsWith(bearerScheme, StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authorization.Substring(bearerScheme.Length).Trim();
                        var exchange = await _tokenExchangeClient.ExchangeToken(token);

                        ctx.HttpContext.Request.Headers.Authorization = $"{bearerScheme}{exchange}";
                    }  
                }
            });    
        }
    }
}
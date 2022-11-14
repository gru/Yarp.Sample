using System.Net.Http.Headers;
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
                    if (authorization.StartsWith(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authorization.Substring(JwtBearerDefaults.AuthenticationScheme.Length).Trim();
                        var exchange = await _tokenExchangeClient.ExchangeToken(token);

                        ctx.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, exchange);
                    }
                }
            });    
        }
    }
}
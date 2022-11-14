using System.Net.Http.Headers;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Model;
using Yarp.Sample;
using Yarp.Sample.Infrastructure;
using Yarp.Sample.Keycloak;
using Yarp.Shared;

var builder = WebApplication.CreateBuilder(args);

var yarpOptions = new YarpSampleOptions();
builder.Configuration
    .GetSection(nameof(YarpSampleOptions))
    .Bind(yarpOptions);
builder.Services
    .Configure<YarpSampleOptions>(builder.Configuration
        .GetSection(nameof(YarpSampleOptions)));

builder.Services
    .AddSingleton<IPassportService, PassportService>();
builder.Services.Configure<PassportOptions>(
    builder.Configuration
        .GetSection(nameof(PassportOptions)));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISessionValidationService, SessionValidationService>();
builder.Services.AddHttpClient<IKeycloakApi, KeycloakApi>(options =>
{
    var uri = new Uri(yarpOptions.Authority);
    
    options.BaseAddress = 
        new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");
    options.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", yarpOptions.AdminToken);
});
builder.Services.AddHttpClient<ITokenExchangeClient, TokenExchangeClient>(options =>
{
    var uri = new Uri(yarpOptions.Authority);

    options.BaseAddress =
        new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");
});

builder.Services
    .AddControllers()
    .AddControllersAsServices();

builder.Services
    .AddAuthorization(options =>
    {
        options.AddPolicy("RequireAuthenticatedUser", policyBuilder =>
        {
            policyBuilder.RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AppAuthenticationSchemes.ValidationScheme);
        });
        options.AddPolicy("RequireIntrospectedUser", policyBuilder =>
        {
            policyBuilder.RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AppAuthenticationSchemes.IntrospectionScheme);
        });
    })
    .AddAuthentication(options =>
    {
        options.DefaultScheme = AppAuthenticationSchemes.CompoundScheme;
        options.DefaultAuthenticateScheme = AppAuthenticationSchemes.CompoundScheme;
        options.DefaultChallengeScheme = AppAuthenticationSchemes.CompoundScheme;
    })
    .AddJwtBearer(AppAuthenticationSchemes.ValidationScheme, options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                RequireAudience = true,
                ValidateAudience = true,
                ValidAudience = yarpOptions.Audience,
                ValidateIssuer = true,
                ValidIssuer = yarpOptions.Issuer,
                ValidateLifetime = true
            };
        options.Authority = yarpOptions.Authority;
        options.Audience = yarpOptions.Audience;
        options.SaveToken = false;
        options.RequireHttpsMetadata = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var validationService = ctx.HttpContext.RequestServices
                    .GetRequiredService<ISessionValidationService>();
                
                var validationResult = await validationService.Validate(ctx.Principal, out var error); 
                if (!validationResult)
                    ctx.Fail(error!);
            }
        };
    })
    .AddOAuth2Introspection(AppAuthenticationSchemes.IntrospectionScheme, options =>
    {
        options.Authority = yarpOptions.Authority;
        options.ClientId = yarpOptions.ClientId;
        options.ClaimsIssuer = yarpOptions.Issuer;
        options.ClientSecret = yarpOptions.ClientSecret;
        options.IntrospectionEndpoint = yarpOptions.IntrospectionEndpoint;
        options.SkipTokensWithDots = false;
        options.EnableCaching = false;
        options.SaveToken = false;
        options.Events = new OAuth2IntrospectionEvents
        {
            OnTokenValidated = async ctx =>
            {
                var validationService = ctx.HttpContext.RequestServices
                    .GetRequiredService<ISessionValidationService>();

                var validationResult = await validationService.Validate(ctx.Principal, out var error); 
                if (!validationResult)
                    ctx.Fail(error!);
            }
        };
    })
    .AddPolicyScheme(AppAuthenticationSchemes.CompoundScheme, AppAuthenticationSchemes.CompoundScheme, options =>
    {
        options.ForwardDefaultSelector = ctx =>
        {
            var endpointFeature = ctx.Features.Get<IEndpointFeature>();
            if (endpointFeature is { Endpoint: RouteEndpoint routeEndpoint })
            {
                var routeModel = routeEndpoint.Metadata.GetMetadata<RouteModel>();
                if (routeModel?.Config.Metadata?.TryGetValue("AuthenticationScheme", out var scheme) ?? false)
                    return scheme;
            }
            
            return AppAuthenticationSchemes.ValidationScheme;
        };
    });
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration
        .GetSection("ReverseProxy"))
    .AddTransforms<TokenExchangeTransformProvider>()
    .AddTransforms<IdentityPropagationTransformProvider>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy()
    .RequireAuthorization();

app.Run();
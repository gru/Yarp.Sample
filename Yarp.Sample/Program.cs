using System.Net.Http.Headers;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Yarp.Sample;
using Yarp.Sample.Infrastructure;
using Yarp.Sample.Keycloak;

var builder = WebApplication.CreateBuilder(args);

var yarpOptions = new YarpSampleOptions();
builder.Configuration
    .GetSection(nameof(YarpSampleOptions))
    .Bind(yarpOptions);

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

builder.Services
    .Configure<IntrospectionOptions>(builder.Configuration
        .GetSection(nameof(IntrospectionOptions)));

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
        options.AddPolicy("RequireTokenExchange", policyBuilder =>
        {
            policyBuilder.RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AppAuthenticationSchemes.ValidationScheme, AppAuthenticationSchemes.ExchangeScheme);
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
            var introspectionOptions = ctx.RequestServices
                .GetRequiredService<IOptions<IntrospectionOptions>>();
            
            var scheme = introspectionOptions.Value.Paths.Any(p => ctx.Request.Path.StartsWithSegments(p)) 
                ? AppAuthenticationSchemes.IntrospectionScheme 
                : AppAuthenticationSchemes.ValidationScheme;

            return scheme;
        };
    });
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration
        .GetSection("ReverseProxy"));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy()
    .RequireAuthorization();

app.Run();
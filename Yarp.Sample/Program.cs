using System.Net.Http.Headers;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Yarp.Sample.Infrastructure;
using Yarp.Sample.Keycloak;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISessionValidationService, SessionValidationService>();
builder.Services.AddHttpClient<IKeycloakApi, KeycloakApi>(options =>
{
    options.BaseAddress = new Uri("http://localhost:8080");
    options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Bearer",
        "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICIzZTRVTy1INkY4VGNvM25qa241WlpDZHlJTzhpOW9tQjZ5RzhsMHhxTFo4In0.eyJleHAiOjE2Njc0MzYwNjUsImlhdCI6MTY2NzQwMDA2NSwianRpIjoiODg5ZTQzYjItOGE5Yi00ODZiLTk5ZWItMDA0N2NkOGY5NGNlIiwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo4MDgwL3JlYWxtcy9tYXN0ZXIiLCJzdWIiOiIwZTcwYTQwMS1hNWExLTQ3NDQtYjY2Ni1jY2YzMDNmZDljMTUiLCJ0eXAiOiJCZWFyZXIiLCJhenAiOiJhZG1pbi1jbGkiLCJzZXNzaW9uX3N0YXRlIjoiMjE5ZjY3YjUtNDNhMC00NDE3LWJmMjktYWY3Yzg3NTcwZmU0IiwiYWNyIjoiMSIsInNjb3BlIjoicHJvZmlsZSBlbWFpbCIsInNpZCI6IjIxOWY2N2I1LTQzYTAtNDQxNy1iZjI5LWFmN2M4NzU3MGZlNCIsImVtYWlsX3ZlcmlmaWVkIjpmYWxzZSwicHJlZmVycmVkX3VzZXJuYW1lIjoiYWRtaW4ifQ.Oid9q7QgRttyLQDrxXA-30484d7lvL3P-pXhTaGMlQBB45u7R-spSW60r4m-GIJnNSuOyzHwlQNnh3ckoTWKAfT-FQ_uB7x6h6cXXD9KfAGsgH2UFYkGIr46IBAT4-ZkqXpOMbpk1gM_WbDa_ChWJY3BnhA8IU3WNHqCcLQm1EPUilGTtm1iW46VvCHPamgN2K5VRCIfECk43mSSynOb2pQsoSaO4p6LE-1ZLU1nJrRfGiMkxpyYw7aWXFjMoWrkL9UtlAARMGOQb7cNtq_GMPNWlfpi7NFR8ddfbYir-kbkpsbcyxLfrYKqwoL16CBw2chOCTlsgTKGLZKsPXLTYQ");
});

builder.Services
    .Configure<IntrospectionOptions>(builder.Configuration
        .GetSection(nameof(IntrospectionOptions)));

builder.Services
    .AddAuthorization(options =>
    {
        options.AddPolicy("RequireAuthenticatedUser", policyBuilder =>
        {
            policyBuilder.RequireAuthenticatedUser();
            policyBuilder.AddAuthenticationSchemes(AppAuthenticationSchemes.ValidationScheme);
        });
        options.AddPolicy("RequireIntrospectedUser", policyBuilder =>
        {
            policyBuilder.RequireAuthenticatedUser();
            policyBuilder.AuthenticationSchemes.Add(AppAuthenticationSchemes.IntrospectionScheme);
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
                ValidAudience = "yarp",
                ValidateIssuer = true,
                ValidIssuer = "http://localhost:8080/realms/yarp",
                ValidateLifetime = true
            };
        options.Authority = "http://localhost:8080/realms/yarp";
        options.Audience = "yarp";
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
        options.Authority = "http://localhost:8080/realms/yarp";
        options.ClientId = "yarp";
        options.ClaimsIssuer = "http://localhost:8080/realms/yarp";
        options.ClientSecret = "RhvfIgLHkHJx15iI7T3XlWqeU3ldkADR";
        options.IntrospectionEndpoint = "http://localhost:8080/realms/yarp/protocol/openid-connect/token/introspect";
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

app.MapPost("/invalidate", async ctx =>
{
    var validationService = ctx.RequestServices
        .GetRequiredService<ISessionValidationService>();
    
    var user = ctx.Request.Query["user"];
    
    await validationService.BlockUser(user);
});
app.MapReverseProxy()
    .RequireAuthorization();

app.Run();
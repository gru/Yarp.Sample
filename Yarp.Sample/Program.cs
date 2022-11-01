using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Yarp.Sample.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
app.MapReverseProxy()
    .RequireAuthorization();
app.Run();

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddAuthorization(options =>
    {
        options.AddPolicy("RequireAuthenticatedUser", policyBuilder =>
        {
            policyBuilder.RequireAuthenticatedUser();
        });
    })
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
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
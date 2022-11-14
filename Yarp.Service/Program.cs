using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.Service;
using Yarp.Service.Infrastructure;
using Yarp.Shared;

var builder = WebApplication.CreateBuilder(args);

var serviceOptions = new AppOptions();
builder.Configuration
    .GetSection(nameof(AppOptions))
    .Bind(serviceOptions);

builder.Services
    .Configure<PassportOptions>(builder.Configuration
        .GetSection(nameof(PassportOptions)))
    .AddControllers()
    .AddControllersAsServices();

builder.Services
    .AddSingleton<IPassportService, PassportService>();

builder.Services
    .AddAuthorization()
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = PassportDefaults.AuthenticationScheme;
    })
    .AddScheme<PassportAuthenticationOptions, PassportAuthenticationHandler>(
        PassportDefaults.AuthenticationScheme, _ =>
    {
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                RequireAudience = false,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false
            };
        options.Authority = serviceOptions.Authority;
        options.Audience = serviceOptions.Audience;
        options.SaveToken = false;
        options.RequireHttpsMetadata = false;
    });

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
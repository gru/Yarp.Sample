using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yarp.Service.Infrastructure;

namespace Yarp.Service.Controllers;

public class AppController : ControllerBase
{
    [HttpGet("/")]
    public string Root()
    {
        return $"Hello, {User.GetName()}";
    }
    
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("/introspection")]
    public string Introspection()
    {
        return $"Hello from introspection, {User.GetName()}";
    }
    
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("/exchange")]
    public string Exchange()
    {
        return $"Hello from exchange, {User.GetName()}";
    }
    
    [Authorize(AuthenticationSchemes = PassportDefaults.AuthenticationScheme)]
    [HttpGet("/passport")]
    public string Passport()
    {
        return $"Hello from passport, {User.GetName()}";
    }
}
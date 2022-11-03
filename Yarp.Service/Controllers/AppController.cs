using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Yarp.Service.Controllers;

public class AppController : ControllerBase
{
    [HttpGet("/")]
    public string Root()
    {
        return $"Hello, {User.GetName()}";
    }
    
    [Authorize]
    [HttpGet("introspection")]
    public string Introspection()
    {
        return $"Hello from introspection, {User.GetName()}";
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yarp.Sample.Infrastructure;

namespace Yarp.Sample.Controllers;

[Route("/")]
[AllowAnonymous]
public class SessionController : ControllerBase
{
    private readonly ISessionValidationService _validationService;

    public SessionController(ISessionValidationService validationService)
    {
        _validationService = validationService;
    }

    [HttpPost("users/{user}/block")]
    public async Task BlockUser(string user)
    {
        await _validationService.BlockUser(user);
    }
}
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TesteController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("API funcionando!");
    }
}
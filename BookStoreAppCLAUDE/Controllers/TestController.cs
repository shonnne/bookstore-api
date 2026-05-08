using Microsoft.AspNetCore.Mvc;

namespace BookStoreApp.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping() =>
        Ok(new { Status = "API is running ✅", Timestamp = DateTime.Now });
}
using Microsoft.AspNetCore.Mvc;
using BookStoreApp.DTOs;
using BookStoreApp.Services;

namespace BookStoreApp.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var (success, message) = await _authService.Register(dto);
        return success ? Ok(message) : BadRequest(message);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (success, message, token, customer) = await _authService.Login(dto);

        if (!success) return Unauthorized(message);

        // Return token + user info to frontend
        return Ok(new
        {
            Message = message,
            Token = token,
            Username = customer!.UserName,
            Email = customer.Email,
            Role = "Customer"
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout([FromQuery] string username)
    {
        // JWT is stateless — logout is handled client side by dropping the token
        return Ok($"User '{username}' logged out successfully.");
    }
}
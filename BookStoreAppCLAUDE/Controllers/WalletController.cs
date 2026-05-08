using Microsoft.AspNetCore.Mvc;
using BookStoreApp.DTOs;
using BookStoreApp.Services;

namespace BookStoreApp.Controllers;

[ApiController]
[Route("api/wallet")]
public class WalletController : ControllerBase
{
    private readonly WalletService _walletService;
    private readonly FileService _fileService;

    public WalletController(WalletService walletService, FileService fileService)
    {
        _walletService = walletService;
        _fileService = fileService;
    }

    // Customer gets their wallet info
    [HttpGet("{username}")]
    public IActionResult GetWallet(string username)
    {
        var wallet = _walletService.GetWallet(username);
        if (wallet == null) return NotFound("Customer not found.");
        return Ok(wallet);
    }

    // Customer submits a money request
    [HttpPost("request")]
    public IActionResult RequestMoney([FromBody] MoneyRequestDto dto)
    {
        // Front end validation backup
        if (dto.Amount < 1)
            return BadRequest("Minimum request amount is $1.");

        if (dto.Amount > 10000)
            return BadRequest("Maximum request amount is $10,000.");

        if (string.IsNullOrWhiteSpace(dto.Username))
            return BadRequest("Username is required.");

        var (success, message) = _walletService.RequestMoney(dto);
        return success ? Ok(message) : BadRequest(message);
    }

    // Admin gets all pending requests
    [HttpGet("requests")]
    public IActionResult GetPendingRequests(
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var requests = _walletService.GetPendingRequests();
        return Ok(requests);
    }

    // Admin approves a request
    [HttpPost("approve/{id}")]
    public async Task<IActionResult> ApproveRequest(
        int id,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword,
        [FromQuery] string? note = null)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var (success, message) = await _walletService.ApproveRequest(id, note ?? "");
        return success ? Ok(message) : BadRequest(message);
    }

    // Admin rejects a request
    [HttpPost("reject/{id}")]
    public IActionResult RejectRequest(
        int id,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword,
        [FromQuery] string? note = null)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var (success, message) = _walletService.RejectRequest(id, note ?? "");
        return success ? Ok(message) : BadRequest(message);
    }
}
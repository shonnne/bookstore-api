using Microsoft.AspNetCore.Mvc;
using BookStoreApp.Services;

namespace BookStoreApp.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly CustomerService _customerService;
    private readonly FileService _fileService;

    public CustomerController(CustomerService customerService, FileService fileService)
    {
        _customerService = customerService;
        _fileService = fileService;
    }

    [HttpGet("{username}")]
    public IActionResult GetCustomer(string username)
    {
        var customer = _customerService.GetByUsername(username);
        if (customer == null) return NotFound("Customer not found.");

        return Ok(new
        {
            customer.UserName,
            customer.Email,
            customer.IsBlocked,
            customer.BlockUntil
        });
    }

    [HttpGet("{username}/loans")]
    public IActionResult GetLoans(string username)
    {
        var loans = _customerService.GetLoans(username);
        return Ok(loans);
    }

    [HttpGet("{username}/purchases")]
    public IActionResult GetPurchases(string username)
    {
        var purchases = _customerService.GetPurchases(username);
        return Ok(purchases);
    }

    // Admin only
    [HttpPost("{username}/block")]
    public IActionResult BlockUser(
        string username,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword,
        [FromQuery] int? days = null)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var (success, message) = _customerService.BlockUser(username, days);
        return success ? Ok(message) : NotFound(message);
    }

    // Admin only
    [HttpPost("{username}/unblock")]
    public IActionResult UnblockUser(
        string username,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var (success, message) = _customerService.UnblockUser(username);
        return success ? Ok(message) : NotFound(message);
    }

    // Admin only – manually trigger overdue check
    [HttpPost("check-overdue")]
    public IActionResult CheckOverdue(
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        int count = _customerService.CheckAndBlockOverdueUsers();
        return Ok($"Overdue check completed. {count} user(s) newly blocked.");
    }
}
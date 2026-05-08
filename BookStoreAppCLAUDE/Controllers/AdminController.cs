using Microsoft.AspNetCore.Mvc;
using BookStoreApp.DTOs;
using BookStoreApp.Services;

namespace BookStoreApp.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly CustomerService _customerService;
    private readonly BookService _bookService;

    public AdminController(AdminService adminService, CustomerService customerService, BookService bookService)
    {
        _adminService = adminService;
        _customerService = customerService;
        _bookService = bookService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        if (!_adminService.ValidateAdmin(dto.Username, dto.Password))
            return Unauthorized("Invalid username or password.");

        return Ok(new { Message = "Admin logged in successfully.", Username = dto.Username });
    }

    [HttpGet("stats")]
    public IActionResult GetStats(
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        return Ok(_adminService.GetStats(_bookService));
    }

    [HttpGet("customers")]
    public IActionResult GetAllCustomers(
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var customers = _adminService.GetAllCustomers()
            .Select(c => new
            {
                c.Id,
                Username = c.UserName,
                c.Email,
                c.IsBlocked,
                c.BlockUntil,
                LoanCount = c.Loans.Count,
                PurchaseCount = c.Purchases.Count
            });

        return Ok(customers);
    }

    [HttpGet("customers/{username}")]
    public IActionResult GetCustomer(
        string username,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var customer = _adminService.GetCustomer(username);
        if (customer == null) return NotFound("Customer not found.");

        return Ok(new
        {
            customer.Id,
            Username = customer.UserName,
            customer.Email,
            customer.IsBlocked,
            customer.BlockUntil,
            customer.Loans,
            customer.Purchases
        });
    }

    [HttpGet("customers/blocked")]
    public IActionResult GetBlockedCustomers(
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var blocked = _adminService.GetBlockedCustomers()
            .Select(c => new
            {
                Username = c.UserName,
                c.Email,
                c.BlockUntil
            });

        return Ok(blocked);
    }

    [HttpPost("customers/{username}/block")]
    public IActionResult BlockUser(
        string username,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword,
        [FromQuery] int? days = null)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var (success, message) = _customerService.BlockUser(username, days);
        return success ? Ok(message) : NotFound(message);
    }

    [HttpPost("customers/{username}/unblock")]
    public IActionResult UnblockUser(
        string username,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var (success, message) = _customerService.UnblockUser(username);
        return success ? Ok(message) : NotFound(message);
    }

    [HttpPost("customers/check-overdue")]
    public IActionResult CheckOverdue(
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        int count = _customerService.CheckAndBlockOverdueUsers();
        return Ok($"Overdue check complete. {count} user(s) newly blocked.");
    }

    [HttpGet("books/overdue")]
    public IActionResult GetOverdueBooks(
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_adminService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var overdue = _bookService.GetOverdueBooks();
        return Ok(overdue);
    }
}
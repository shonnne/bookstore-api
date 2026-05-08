using BookStoreApp.Data;
using BookStoreApp.DTOs;
using BookStoreApp.Helpers;
using BookStoreApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApp.Services;

public class AuthService
{
    private readonly UserManager<Customer> _userManager;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(UserManager<Customer> userManager, AppDbContext context, IConfiguration config)
    {
        _userManager = userManager;
        _context = context;
        _config = config;
    }

    public async Task<(bool Success, string Message)> Register(RegisterDto dto)
    {
        // Front end validation backup
        if (!ValidationHelper.IsValidUsername(dto.Username))
            return (false, "Invalid username. Must be 3–20 characters, letters/numbers/underscore only.");

        if (!ValidationHelper.IsValidPassword(dto.Password))
            return (false, "Invalid password. Must be at least 6 characters.");

        if (!ValidationHelper.IsValidEmail(dto.Email))
            return (false, "Invalid email format.");

        // Check if username already taken
        var existingUser = await _userManager.FindByNameAsync(dto.Username);
        if (existingUser != null)
            return (false, "Username already taken.");

        // Check if email already taken
        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null)
            return (false, "Email already in use.");

        var customer = new Customer
        {
            UserName = dto.Username,
            Email = dto.Email,
            IsBlocked = false
        };

        // Identity handles password hashing internally
        var result = await _userManager.CreateAsync(customer, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, errors);
        }

        // Assign Customer role
        await _userManager.AddToRoleAsync(customer, "Customer");

        return (true, "Registration successful.");
    }

    public async Task<(bool Success, string Message, string? Token, Customer? Customer)> Login(LoginDto dto)
    {
        // Find user by username
        var customer = await _userManager.Users
            .Include(c => c.Loans)
            .FirstOrDefaultAsync(c => c.UserName == dto.Username);

        if (customer == null)
            return (false, "Invalid username or password.", null, null);

        // Identity checks the password
        var passwordValid = await _userManager.CheckPasswordAsync(customer, dto.Password);
        if (!passwordValid)
            return (false, "Invalid username or password.", null, null);

        // Auto-block if customer has overdue loans
        var hasOverdue = customer.Loans.Any(l => !l.IsReturned && l.DueDate < DateTime.Now);
        if (hasOverdue && !customer.IsBlocked)
        {
            customer.IsBlocked = true;
            customer.BlockUntil = null;
            await _userManager.UpdateAsync(customer);
        }

        // Auto-unblock if BlockUntil has passed
        if (customer.IsBlocked && customer.BlockUntil.HasValue && DateTime.Now >= customer.BlockUntil)
        {
            customer.IsBlocked = false;
            customer.BlockUntil = null;
            await _userManager.UpdateAsync(customer);
        }

        if (customer.IsBlocked)
        {
            var until = customer.BlockUntil.HasValue
                ? $"Blocked until: {customer.BlockUntil:yyyy-MM-dd HH:mm}"
                : "Blocked until overdue books are returned.";

            return (false, $"Account is blocked. {until}", null, null);
        }

        // Generate JWT token with Customer role
        var token = JwtHelper.GenerateToken(customer.UserName!, "Customer", _config);

        return (true, "Login successful.", token, customer);
    }
}
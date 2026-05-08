using BookStoreApp.Data;
using BookStoreApp.DTOs;
using BookStoreApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApp.Services;

public class WalletService
{
    private readonly AppDbContext _context;
    private readonly UserManager<Customer> _userManager;

    public WalletService(AppDbContext context, UserManager<Customer> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Get customer balance and request history
    public object? GetWallet(string username)
    {
        var customer = _userManager.Users
            .Include(c => c.MoneyRequests)
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null) return null;

        return new
        {
            Balance = customer.Balance,
            Requests = customer.MoneyRequests
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new
                {
                    r.Id,
                    r.Amount,
                    r.Status,
                    r.RequestDate,
                    r.ResolvedDate,
                    r.AdminNote
                })
        };
    }

    // Customer submits a money request
    public (bool Success, string Message) RequestMoney(MoneyRequestDto dto)
    {
        // Validation
        if (dto.Amount < 1)
            return (false, "Minimum request amount is $1.");

        if (dto.Amount > 10000)
            return (false, "Maximum request amount is $10,000.");

        var customer = _userManager.Users
            .Include(c => c.MoneyRequests)
            .FirstOrDefault(c => c.UserName == dto.Username);

        if (customer == null)
            return (false, "Customer not found.");

        // Check if customer already has a pending request
        var hasPending = customer.MoneyRequests
            .Any(r => r.Status == "Pending");

        if (hasPending)
            return (false, "You already have a pending request. Wait for admin to resolve it first.");

        var request = new MoneyRequest
        {
            CustomerId = customer.Id,
            Amount = dto.Amount,
            Status = "Pending",
            RequestDate = DateTime.Now
        };

        _context.MoneyRequests.Add(request);
        _context.SaveChanges();

        return (true, $"Request for ${dto.Amount} submitted successfully.");
    }

    // Admin gets all pending requests
    public List<object> GetPendingRequests()
    {
        return _context.MoneyRequests
            .Include(r => r.Customer)
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.RequestDate)
            .Select(r => (object)new
            {
                r.Id,
                Username = r.Customer.UserName,
                r.Amount,
                r.RequestDate
            })
            .ToList();
    }

    // Admin approves a request
    public async Task<(bool Success, string Message)> ApproveRequest(int requestId, string adminNote = "")
    {
        var request = _context.MoneyRequests
            .Include(r => r.Customer)
            .FirstOrDefault(r => r.Id == requestId);

        if (request == null)
            return (false, "Request not found.");

        if (request.Status != "Pending")
            return (false, "Request is no longer pending.");

        // Add money to customer balance
        request.Customer.Balance += request.Amount;
        request.Status = "Approved";
        request.ResolvedDate = DateTime.Now;
        request.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Approved" : adminNote;

        await _userManager.UpdateAsync(request.Customer);
        _context.SaveChanges();

        return (true, $"Request approved. ${request.Amount} added to {request.Customer.UserName}'s balance.");
    }

    // Admin rejects a request
    public (bool Success, string Message) RejectRequest(int requestId, string adminNote = "")
    {
        var request = _context.MoneyRequests
            .Include(r => r.Customer)
            .FirstOrDefault(r => r.Id == requestId);

        if (request == null)
            return (false, "Request not found.");

        if (request.Status != "Pending")
            return (false, "Request is no longer pending.");

        request.Status = "Rejected";
        request.ResolvedDate = DateTime.Now;
        request.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Rejected" : adminNote;

        _context.SaveChanges();

        return (true, $"Request rejected.");
    }

    // Deduct balance when buying or loaning
    public (bool Success, string Message) DeductBalance(string username, decimal amount, string reason)
    {
        var customer = _userManager.Users
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null)
            return (false, "Customer not found.");

        if (customer.Balance < amount)
            return (false, $"Insufficient balance. You need ${amount} but have ${customer.Balance}.");

        customer.Balance -= amount;
        _userManager.UpdateAsync(customer).Wait();

        return (true, $"${amount} deducted for {reason}.");
    }

    public void RefundBalance(string username, decimal amount)
    {
        var customer = _userManager.Users
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null) return;

        customer.Balance += amount;
        _userManager.UpdateAsync(customer).Wait();
    }

}
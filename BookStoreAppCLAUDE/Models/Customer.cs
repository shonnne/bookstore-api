using Microsoft.AspNetCore.Identity;
using BookStoreApp.Models;

namespace BookStoreApp.Models;

public class Customer : IdentityUser
{
    public bool IsBlocked { get; set; } = false;
    public DateTime? BlockUntil { get; set; }
    public decimal Balance { get; set; } = 0; // ← new

    public List<Loan> Loans { get; set; } = new();
    public List<Purchase> Purchases { get; set; } = new();
    public List<MoneyRequest> MoneyRequests { get; set; } = new(); // ← new
}
using BookStoreApp.Data;
using BookStoreApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApp.Services;

public class AdminService
{
    private readonly FileService _fileService;
    private readonly AppDbContext _context;
    private readonly UserManager<Customer> _userManager;

    public AdminService(FileService fileService, AppDbContext context, UserManager<Customer> userManager)
    {
        _fileService = fileService;
        _context = context;
        _userManager = userManager;
    }

    public bool ValidateAdmin(string username, string password)
    {
        return _fileService.ValidateAdmin(username, password);
    }

    public List<Customer> GetAllCustomers()
    {
        return _userManager.Users
            .Include(c => c.Loans)
            .Include(c => c.Purchases)
            .ToList();
    }

    public Customer? GetCustomer(string username)
    {
        return _userManager.Users
            .Include(c => c.Loans)
            .Include(c => c.Purchases)
            .FirstOrDefault(c => c.UserName == username);
    }

    public List<Customer> GetBlockedCustomers()
    {
        return _userManager.Users
            .Where(c => c.IsBlocked)
            .ToList();
    }

    public object GetStats(BookService bookService)
    {
        var books = bookService.GetAll();
        var customers = _userManager.Users.Count();
        var blocked = _userManager.Users.Count(c => c.IsBlocked);
        var loans = _context.Loans.Count(l => !l.IsReturned);
        var purchases = _context.Purchases.Count();

        return new
        {
            TotalBooks = books.Count,
            AvailableBooks = books.Count(b => b.IsAvailable),
            LoanedBooks = books.Count(b => b.IsLoaned),
            SoldBooks = books.Count(b => b.BoughtBy != null),
            TotalCustomers = customers,
            BlockedCustomers = blocked,
            ActiveLoans = loans,
            TotalPurchases = purchases
        };
    }
}
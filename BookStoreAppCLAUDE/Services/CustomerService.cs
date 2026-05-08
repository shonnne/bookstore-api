using BookStoreApp.Data;
using BookStoreApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApp.Services;

public class CustomerService
{
    private readonly AppDbContext _context;
    private readonly BookService _bookService;
    private readonly UserManager<Customer> _userManager;

    public CustomerService(AppDbContext context, BookService bookService, UserManager<Customer> userManager)
    {
        _context = context;
        _bookService = bookService;
        _userManager = userManager;
    }

    public Customer? GetByUsername(string username) =>
        _userManager.Users
            .Include(c => c.Loans)
            .Include(c => c.Purchases)
            .FirstOrDefault(c => c.UserName == username);

    public bool IsBlocked(string username)
    {
        var customer = _userManager.Users
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null || !customer.IsBlocked) return false;

        // Auto-unblock if time has passed
        if (customer.BlockUntil.HasValue && DateTime.Now >= customer.BlockUntil)
        {
            customer.IsBlocked = false;
            customer.BlockUntil = null;
            _userManager.UpdateAsync(customer).Wait();
            return false;
        }

        return true;
    }

    public (bool Success, string Message) BlockUser(string username, int? days = null)
    {
        var customer = _userManager.Users
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null) return (false, "Customer not found.");

        customer.IsBlocked = true;
        customer.BlockUntil = days.HasValue ? DateTime.Now.AddDays(days.Value) : null;

        _userManager.UpdateAsync(customer).Wait();

        var msg = days.HasValue ? $"for {days} days" : "permanently";
        return (true, $"User '{username}' has been blocked {msg}.");
    }

    public (bool Success, string Message) UnblockUser(string username)
    {
        var customer = _userManager.Users
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null) return (false, "Customer not found.");

        customer.IsBlocked = false;
        customer.BlockUntil = null;

        _userManager.UpdateAsync(customer).Wait();
        return (true, $"User '{username}' has been unblocked.");
    }

    public List<Loan> GetLoans(string username)
    {
        var customer = _userManager.Users
            .Include(c => c.Loans)
            .FirstOrDefault(c => c.UserName == username);

        return customer?.Loans ?? new List<Loan>();
    }

    public List<Purchase> GetPurchases(string username)
    {
        var customer = _userManager.Users
            .Include(c => c.Purchases)
            .FirstOrDefault(c => c.UserName == username);

        return customer?.Purchases ?? new List<Purchase>();
    }

    public void AddLoanRecord(string username, Book book)
    {
        var customer = _userManager.Users
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null) return;

        _context.Loans.Add(new Loan
        {
            CustomerId = customer.Id,
            BookId = book.Id,
            BookName = book.Name,
            BookAuthor = book.Author,
            LoanDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(14)
        });

        _context.SaveChanges();
    }

    public void ReturnLoanRecord(string username, int bookId)
    {
        var customer = _userManager.Users
            .Include(c => c.Loans)
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null) return;

        var loan = _context.Loans.FirstOrDefault(l =>
            l.CustomerId == customer.Id && l.BookId == bookId && !l.IsReturned);

        if (loan != null)
        {
            loan.IsReturned = true;
            loan.ReturnDate = DateTime.Now;
        }

        // Auto-unblock if no more overdue loans
        var stillHasOverdue = customer.Loans
            .Any(l => !l.IsReturned && l.DueDate < DateTime.Now);

        if (!stillHasOverdue && customer.IsBlocked && customer.BlockUntil == null)
        {
            customer.IsBlocked = false;
            customer.BlockUntil = null;
            _userManager.UpdateAsync(customer).Wait();
        }

        _context.SaveChanges();
    }

    public void AddPurchaseRecord(string username, Book book)
    {
        var customer = _userManager.Users
            .FirstOrDefault(c => c.UserName == username);

        if (customer == null) return;

        _context.Purchases.Add(new Purchase
        {
            CustomerId = customer.Id,
            BookId = book.Id,
            BookName = book.Name,
            BookAuthor = book.Author,
            Price = book.Price,
            PurchaseDate = DateTime.Now
        });

        _context.SaveChanges();
    }

    public int CheckAndBlockOverdueUsers()
    {
        var overdueBooks = _bookService.GetOverdueBooks();
        int blocked = 0;

        foreach (var book in overdueBooks)
        {
            if (book.LoanedBy == null) continue;

            var customer = _userManager.Users
                .FirstOrDefault(c => c.UserName == book.LoanedBy);

            if (customer != null && !customer.IsBlocked)
            {
                customer.IsBlocked = true;
                customer.BlockUntil = DateTime.Now.AddDays(7);
                _userManager.UpdateAsync(customer).Wait();
                blocked++;
            }
        }

        return blocked;
    }
}
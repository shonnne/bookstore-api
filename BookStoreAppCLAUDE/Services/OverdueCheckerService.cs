using BookStoreApp.Data;
using BookStoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApp.Services;

public class OverdueCheckerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueCheckerService> _logger;

    public OverdueCheckerService(IServiceScopeFactory scopeFactory, ILogger<OverdueCheckerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Overdue Checker Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CheckOverdueLoans();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during overdue check.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private void CheckOverdueLoans()
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookService = scope.ServiceProvider.GetRequiredService<BookService>();

        var overdueLoans = context.Loans
            .Include(l => l.Customer)
            .Where(l => !l.IsReturned && l.DueDate < DateTime.Now)
            .ToList();

        if (!overdueLoans.Any())
        {
            _logger.LogInformation("Overdue check ran — no overdue loans found.");
            return;
        }

        int blockedCount = 0;

        foreach (var loan in overdueLoans)
        {
            var customer = loan.Customer;
            if (customer == null || customer.IsBlocked) continue;

            customer.IsBlocked = true;
            customer.BlockUntil = null;
            blockedCount++;

            _logger.LogWarning(
                "Customer '{UserName}' blocked. Overdue book: '{BookName}' (due {DueDate:yyyy-MM-dd}).",
                customer.UserName, loan.BookName, loan.DueDate
            );
        }

        if (blockedCount > 0)
        {
            context.SaveChanges();
            _logger.LogInformation("Overdue check complete. {Count} user(s) newly blocked.", blockedCount);
        }
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BookStoreApp.Models;

namespace BookStoreApp.Data;

public class AppDbContext : IdentityDbContext<Customer>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Loan> Loans { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<MoneyRequest> MoneyRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Loan>()
            .HasOne(l => l.Customer)
            .WithMany(c => c.Loans)
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Customer)
            .WithMany(c => c.Purchases)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MoneyRequest>()
            .HasOne(m => m.Customer)
            .WithMany(c => c.MoneyRequests)
            .HasForeignKey(m => m.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Fix decimal precision ─────────────────────────────
        // (18, 2) means up to 18 digits total, 2 after decimal point
        // This prevents silent truncation of money values
        modelBuilder.Entity<Customer>()
            .Property(c => c.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Purchase>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<MoneyRequest>()
            .Property(m => m.Amount)
            .HasPrecision(18, 2);
    }
}
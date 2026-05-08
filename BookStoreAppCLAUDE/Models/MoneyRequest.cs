using System.Text.Json.Serialization;

namespace BookStoreApp.Models;

public class MoneyRequest
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = "";

    [JsonIgnore]
    public Customer Customer { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime RequestDate { get; set; } = DateTime.Now;
    public DateTime? ResolvedDate { get; set; }
    public string? AdminNote { get; set; }
}
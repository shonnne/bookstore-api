using System.Text.Json.Serialization;

namespace BookStoreApp.Models;

public class Purchase
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = ""; // changed int → string

    [JsonIgnore]
    public Customer Customer { get; set; } = null!;

    public int BookId { get; set; }
    public string BookName { get; set; } = "";
    public string BookAuthor { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime PurchaseDate { get; set; }
}
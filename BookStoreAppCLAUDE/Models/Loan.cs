using System.Text.Json.Serialization;

namespace BookStoreApp.Models;

public class Loan
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = ""; // changed int → string

    [JsonIgnore]
    public Customer Customer { get; set; } = null!;

    public int BookId { get; set; }
    public string BookName { get; set; } = "";
    public string BookAuthor { get; set; } = "";
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsReturned { get; set; } = false;
    public DateTime? ReturnDate { get; set; }
}
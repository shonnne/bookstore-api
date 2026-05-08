namespace BookStoreApp.Models;

public class Book
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public decimal LoanPrice { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsLoaned { get; set; } = false;
    public string? LoanedBy { get; set; }
    public DateTime? LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? BoughtBy { get; set; }
}
namespace BookStoreApp.Models;

// Response wrapper for book list with metadata
public class Books
{
    public int TotalCount => Items.Count;
    public int AvailableCount => Items.Count(b => b.IsAvailable);
    public int LoanedCount => Items.Count(b => b.IsLoaned);
    public int SoldCount => Items.Count(b => b.BoughtBy != null);
    public List<Book> Items { get; set; } = new();
}
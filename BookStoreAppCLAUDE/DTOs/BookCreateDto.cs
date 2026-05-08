namespace BookStoreApp.DTOs;

public class BookCreateDto
{
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public decimal LoanPrice { get; set; }
}
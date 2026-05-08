namespace BookStoreApp.DTOs;

public class BookUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public decimal LoanPrice { get; set; }
}
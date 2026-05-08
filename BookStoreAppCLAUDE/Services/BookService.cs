using BookStoreApp.DTOs;
using BookStoreApp.Helpers;
using BookStoreApp.Models;

namespace BookStoreApp.Services;

public class BookService
{
    private readonly FileService _fileService;

    public BookService(FileService fileService)
    {
        _fileService = fileService;
    }

    public List<Book> GetAll() => _fileService.LoadBooks();

    public Book? GetById(int id) =>
        _fileService.LoadBooks().FirstOrDefault(b => b.Id == id);

    public Book Add(BookCreateDto dto)
    {
        var books = _fileService.LoadBooks();

        var book = new Book
        {
            Id = DataHelper.GenerateId(books),
            Name = dto.Name,
            Author = dto.Author,
            Price = dto.Price,
            LoanPrice = dto.LoanPrice,
            IsAvailable = true,
            IsLoaned = false
        };

        books.Add(book);
        _fileService.SaveBooks(books);
        return book;
    }

    public bool Update(BookUpdateDto dto)
    {
        var books = _fileService.LoadBooks();
        var book = books.FirstOrDefault(b => b.Id == dto.Id);
        if (book == null) return false;

        book.Name = dto.Name;
        book.Author = dto.Author;
        book.Price = dto.Price;
        book.LoanPrice = dto.LoanPrice;

        _fileService.SaveBooks(books);
        return true;
    }

    public bool Delete(int id)
    {
        var books = _fileService.LoadBooks();
        var book = books.FirstOrDefault(b => b.Id == id);
        if (book == null) return false;

        books.Remove(book);
        _fileService.SaveBooks(books);
        return true;
    }

    // Returns the updated book so the controller can create a SQL record
    public Book? BorrowBook(int id, string username)
    {
        var books = _fileService.LoadBooks();
        var book = books.FirstOrDefault(b => b.Id == id);

        if (book == null || book.IsLoaned || !book.IsAvailable)
            return null;

        book.IsLoaned = true;
        book.IsAvailable = false;
        book.LoanedBy = username;
        book.LoanDate = DateTime.Now;
        book.DueDate = DateTime.Now.AddDays(14);

        _fileService.SaveBooks(books);
        return book;
    }

    public Book? ReturnBook(int id)
    {
        var books = _fileService.LoadBooks();
        var book = books.FirstOrDefault(b => b.Id == id);

        if (book == null || !book.IsLoaned)
            return null;

        var previousBorrower = book.LoanedBy;

        book.IsLoaned = false;
        book.IsAvailable = true;
        book.LoanedBy = null;
        book.LoanDate = null;
        book.DueDate = null;

        _fileService.SaveBooks(books);
        return book;
    }

    public Book? BuyBook(int id, string username)
    {
        var books = _fileService.LoadBooks();
        var book = books.FirstOrDefault(b => b.Id == id);

        if (book == null || !book.IsAvailable || book.IsLoaned)
            return null;

        book.IsAvailable = false;
        book.BoughtBy = username;

        _fileService.SaveBooks(books);
        return book;
    }

    public bool RestockBook(int id)
    {
        var books = _fileService.LoadBooks();
        var book = books.FirstOrDefault(b => b.Id == id);

        if (book == null || book.BoughtBy == null) return false;

        book.BoughtBy = null;
        book.IsAvailable = true;

        _fileService.SaveBooks(books);
        return true;
    }

    public List<Book> GetBorrowedByUser(string username) =>
        _fileService.LoadBooks()
            .Where(b => b.LoanedBy == username && b.IsLoaned)
            .ToList();

    public List<Book> GetBoughtByUser(string username) =>
        _fileService.LoadBooks()
            .Where(b => b.BoughtBy == username)
            .ToList();

    public List<Book> GetOverdueBooks() =>
        _fileService.LoadBooks()
            .Where(b => b.IsLoaned && b.DueDate.HasValue && b.DueDate < DateTime.Now)
            .ToList();
}
using Microsoft.AspNetCore.Mvc;
using BookStoreApp.DTOs;
using BookStoreApp.Models;
using BookStoreApp.Services;

namespace BookStoreApp.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly BookService _bookService;
    private readonly CustomerService _customerService;
    private readonly FileService _fileService;
    private readonly WalletService _walletService;

    public BooksController(BookService bookService, CustomerService customerService, FileService fileService, WalletService walletService)
    {
        _bookService = bookService;
        _customerService = customerService;
        _fileService = fileService;
        _walletService = walletService;
    }


    [HttpGet]
    public IActionResult GetAll()
    {
        var result = new Books { Items = _bookService.GetAll() };
        return Ok(result);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var book = _bookService.GetById(id);
        return book == null ? NotFound("Book not found.") : Ok(book);
    }

    // Admin only
    [HttpPost]
    public IActionResult Add(
        [FromBody] BookCreateDto dto,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var book = _bookService.Add(dto);
        return Ok(book);
    }

    // Admin only
    [HttpPut]
    public IActionResult Update(
        [FromBody] BookUpdateDto dto,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        return _bookService.Update(dto) ? Ok("Book updated.") : NotFound("Book not found.");
    }

    // Admin only
    [HttpDelete("{id}")]
    public IActionResult Delete(
        int id,
        [FromQuery] string adminUsername,
        [FromQuery] string adminPassword)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        return _bookService.Delete(id) ? Ok("Book deleted.") : NotFound("Book not found.");
    }

    [HttpPost("borrow/{id}")]
    public IActionResult Borrow(int id, [FromQuery] string username)
    {
        if (_customerService.IsBlocked(username))
            return BadRequest("User is blocked and cannot borrow books.");

        var book = _bookService.GetById(id);
        if (book == null) return NotFound("Book not found.");

        var (canAfford, balanceMessage) = _walletService.DeductBalance(username, book.LoanPrice, $"loan of '{book.Name}'");
        if (!canAfford) return BadRequest(balanceMessage);

        var borrowedBook = _bookService.BorrowBook(id, username);
        if (borrowedBook == null)
        {
            _walletService.RefundBalance(username, book.LoanPrice); // refund
            return BadRequest("Book is not available for borrowing.");
        }

        _customerService.AddLoanRecord(username, borrowedBook);
        return Ok(new { Message = "Book borrowed successfully.", borrowedBook.DueDate });
    }

    [HttpPost("return/{id}")]
    public IActionResult Return(int id, [FromQuery] string username)
    {
        var book = _bookService.GetById(id);

        // If book was deleted, just clean up the loan record
        if (book == null)
        {
            _customerService.ReturnLoanRecord(username, id);
            return Ok("Book returned successfully.");
        }

        if (book.LoanedBy != username)
            return BadRequest("This book was not borrowed by this user.");

        var returned = _bookService.ReturnBook(id);
        if (returned == null) return BadRequest("Book cannot be returned.");

        _customerService.ReturnLoanRecord(username, id);
        return Ok("Book returned successfully.");
    }

    [HttpPost("buy/{id}")]
    public IActionResult Buy(int id, [FromQuery] string username)
    {
        if (_customerService.IsBlocked(username))
            return BadRequest("User is blocked and cannot buy books.");

        var book = _bookService.GetById(id);
        if (book == null) return NotFound("Book not found.");

        var (canAfford, balanceMessage) = _walletService.DeductBalance(username, book.Price, $"purchase of '{book.Name}'");
        if (!canAfford) return BadRequest(balanceMessage);

        var boughtBook = _bookService.BuyBook(id, username);
        if (boughtBook == null)
        {
            _walletService.RefundBalance(username, book.Price); // refund
            return BadRequest("Book is not available for purchase.");
        }

        _customerService.AddPurchaseRecord(username, boughtBook);
        return Ok(new { Message = "Book purchased successfully.", boughtBook.Name, boughtBook.Price });
    }

    [HttpGet("borrowed")]
    public IActionResult GetBorrowedByUser([FromQuery] string username)
    {
        return Ok(_bookService.GetBorrowedByUser(username));
    }

    [HttpGet("bought")]
    public IActionResult GetBoughtByUser([FromQuery] string username)
    {
        return Ok(_bookService.GetBoughtByUser(username));
    }

    [HttpPost("restock/{id}")]
    public IActionResult Restock(
    int id,
    [FromQuery] string adminUsername,
    [FromQuery] string adminPassword)
    {
        if (!_fileService.ValidateAdmin(adminUsername, adminPassword))
            return Unauthorized("Admin access required.");

        var result = _bookService.RestockBook(id);

        if (!result) return BadRequest("Book cannot be restocked.");

        return Ok("Book restocked successfully.");
    }

}
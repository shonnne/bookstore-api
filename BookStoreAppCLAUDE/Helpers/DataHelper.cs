using BookStoreApp.Models;

namespace BookStoreApp.Helpers;

public static class DataHelper
{
    // Always generates a safe unique ID (max + 1), never repeats
    public static int GenerateId(List<Book> books)
    {
        return books.Count == 0 ? 1 : books.Max(b => b.Id) + 1;
    }
}
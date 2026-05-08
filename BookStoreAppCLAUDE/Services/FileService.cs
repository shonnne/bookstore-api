using System.Text.Json;
using BookStoreApp.Models;

namespace BookStoreApp.Services;

public class FileService
{
    private readonly string _booksPath = "Data/books.json";
    private readonly string _adminsPath = "Data/admins.txt";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    // ── BOOKS (JSON) ──────────────────────────────────────────

    public List<Book> LoadBooks()
    {
        if (!File.Exists(_booksPath)) return new List<Book>();

        var json = File.ReadAllText(_booksPath);

        if (string.IsNullOrWhiteSpace(json)) return new List<Book>();

        return JsonSerializer.Deserialize<List<Book>>(json, _jsonOptions) ?? new List<Book>();
    }

    public void SaveBooks(List<Book> books)
    {
        var json = JsonSerializer.Serialize(books, _jsonOptions);
        File.WriteAllText(_booksPath, json);
    }

    // ── ADMINS (TXT) ─────────────────────────────────────────
    // Format per line: username:password

    public bool ValidateAdmin(string username, string password)
    {
        if (!File.Exists(_adminsPath)) return false;

        foreach (var line in File.ReadAllLines(_adminsPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(':');
            if (parts.Length != 2) continue;

            if (parts[0].Trim() == username && parts[1].Trim() == password)
                return true;
        }

        return false;
    }

    public bool AddAdmin(string username, string password)
    {
        if (ValidateAdmin(username, password)) return false;

        File.AppendAllText(_adminsPath, $"{username}:{password}{Environment.NewLine}");
        return true;
    }
}
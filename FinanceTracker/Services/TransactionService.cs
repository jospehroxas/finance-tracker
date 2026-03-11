using System.Text;
using FinanceTracker.Data;
using FinanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _db;

    public TransactionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Transaction>> GetTransactionsAsync(
        string userId,
        int? year = null,
        int? month = null,
        string? category = null,
        TransactionType? type = null)
    {
        var query = _db.Transactions
            .Where(t => t.UserId == userId)
            .AsQueryable();

        if (year.HasValue)
            query = query.Where(t => t.Date.Year == year.Value);

        if (month.HasValue)
            query = query.Where(t => t.Date.Month == month.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id, string userId)
    {
        return await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(Transaction transaction)
    {
        _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null) return false;

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<MonthlySummary> GetMonthlySummaryAsync(string userId, int year, int month)
    {
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && t.Date.Year == year && t.Date.Month == month)
            .ToListAsync();

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var expensesByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var incomeByCategory = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var recent = transactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(10)
            .ToList();

        return new MonthlySummary(
            year, month,
            totalIncome, totalExpenses,
            totalIncome - totalExpenses,
            expensesByCategory, incomeByCategory,
            recent);
    }

    public async Task<byte[]> ExportToCsvAsync(string userId, int? year = null, int? month = null)
    {
        var transactions = await GetTransactionsAsync(userId, year, month);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Type,Category,Amount,Notes");

        foreach (var t in transactions)
        {
            var notes = t.Notes?.Replace("\"", "\"\"") ?? "";
            sb.AppendLine($"{t.Date:yyyy-MM-dd},{t.Type},{t.Category},{t.Amount:F2},\"{notes}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

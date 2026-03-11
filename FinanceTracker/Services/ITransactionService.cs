using FinanceTracker.Models;

namespace FinanceTracker.Services;

public record MonthlySummary(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetSavings,
    Dictionary<string, decimal> ExpensesByCategory,
    Dictionary<string, decimal> IncomeByCategory,
    List<Transaction> RecentTransactions
);

public interface ITransactionService
{
    Task<List<Transaction>> GetTransactionsAsync(
        string userId,
        int? year = null,
        int? month = null,
        string? category = null,
        TransactionType? type = null);

    Task<Transaction?> GetByIdAsync(int id, string userId);

    Task<Transaction> CreateAsync(Transaction transaction);

    Task<Transaction> UpdateAsync(Transaction transaction);

    Task<bool> DeleteAsync(int id, string userId);

    Task<MonthlySummary> GetMonthlySummaryAsync(string userId, int year, int month);

    Task<byte[]> ExportToCsvAsync(string userId, int? year = null, int? month = null);
}

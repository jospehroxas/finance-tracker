using FinanceTracker.Data;
using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests;

public class TransactionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly TransactionService _service;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _service = new TransactionService(_db);
    }

    // ── GetMonthlySummaryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetMonthlySummary_EmptyMonth_ReturnsZeroTotals()
    {
        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 1);

        Assert.Equal(0, summary.TotalIncome);
        Assert.Equal(0, summary.TotalExpenses);
        Assert.Equal(0, summary.NetSavings);
        Assert.Empty(summary.RecentTransactions);
    }

    [Fact]
    public async Task GetMonthlySummary_AllIncome_ReturnsPositiveNet()
    {
        await SeedTransactions(
            (1000m, TransactionType.Income, "Salary", new DateTime(2024, 3, 1)),
            (500m, TransactionType.Income, "Freelance", new DateTime(2024, 3, 15))
        );

        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 3);

        Assert.Equal(1500m, summary.TotalIncome);
        Assert.Equal(0m, summary.TotalExpenses);
        Assert.Equal(1500m, summary.NetSavings);
    }

    [Fact]
    public async Task GetMonthlySummary_AllExpenses_ReturnsNegativeNet()
    {
        await SeedTransactions(
            (300m, TransactionType.Expense, "Food & Dining", new DateTime(2024, 3, 5)),
            (200m, TransactionType.Expense, "Housing", new DateTime(2024, 3, 10))
        );

        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 3);

        Assert.Equal(0m, summary.TotalIncome);
        Assert.Equal(500m, summary.TotalExpenses);
        Assert.Equal(-500m, summary.NetSavings);
    }

    [Fact]
    public async Task GetMonthlySummary_MixedTransactions_CalculatesCorrectly()
    {
        await SeedTransactions(
            (4000m, TransactionType.Income, "Salary", new DateTime(2024, 3, 1)),
            (1200m, TransactionType.Expense, "Housing", new DateTime(2024, 3, 2)),
            (400m, TransactionType.Expense, "Food & Dining", new DateTime(2024, 3, 10)),
            (800m, TransactionType.Income, "Freelance", new DateTime(2024, 3, 20))
        );

        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 3);

        Assert.Equal(4800m, summary.TotalIncome);
        Assert.Equal(1600m, summary.TotalExpenses);
        Assert.Equal(3200m, summary.NetSavings);
    }

    [Fact]
    public async Task GetMonthlySummary_GroupsExpensesByCategory()
    {
        await SeedTransactions(
            (100m, TransactionType.Expense, "Food & Dining", new DateTime(2024, 3, 1)),
            (200m, TransactionType.Expense, "Food & Dining", new DateTime(2024, 3, 15)),
            (500m, TransactionType.Expense, "Housing", new DateTime(2024, 3, 5))
        );

        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 3);

        Assert.Equal(2, summary.ExpensesByCategory.Count);
        Assert.Equal(300m, summary.ExpensesByCategory["Food & Dining"]);
        Assert.Equal(500m, summary.ExpensesByCategory["Housing"]);
    }

    [Fact]
    public async Task GetMonthlySummary_OnlyReturnsCurrentUserData()
    {
        // Other user's transaction
        _db.Transactions.Add(new Transaction
        {
            Amount = 9999m, Type = TransactionType.Income,
            Category = "Salary", Date = new DateTime(2024, 3, 1),
            UserId = OtherUserId
        });
        await _db.SaveChangesAsync();

        await SeedTransactions(
            (500m, TransactionType.Income, "Freelance", new DateTime(2024, 3, 10))
        );

        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 3);

        Assert.Equal(500m, summary.TotalIncome);
    }

    [Fact]
    public async Task GetMonthlySummary_RecentTransactionsLimitedToTen()
    {
        for (int i = 1; i <= 15; i++)
        {
            _db.Transactions.Add(new Transaction
            {
                Amount = i * 10m, Type = TransactionType.Expense,
                Category = "Shopping", Date = new DateTime(2024, 3, i),
                UserId = TestUserId
            });
        }
        await _db.SaveChangesAsync();

        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 3);

        Assert.Equal(10, summary.RecentTransactions.Count);
    }

    [Fact]
    public async Task GetMonthlySummary_DoesNotIncludeOtherMonths()
    {
        await SeedTransactions(
            (1000m, TransactionType.Income, "Salary", new DateTime(2024, 2, 1)),
            (500m, TransactionType.Income, "Salary", new DateTime(2024, 3, 1)),
            (200m, TransactionType.Income, "Salary", new DateTime(2024, 4, 1))
        );

        var summary = await _service.GetMonthlySummaryAsync(TestUserId, 2024, 3);

        Assert.Equal(500m, summary.TotalIncome);
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsTransaction()
    {
        var t = new Transaction
        {
            Amount = 250m, Type = TransactionType.Expense,
            Category = "Entertainment", Date = DateTime.Today,
            UserId = TestUserId
        };

        var result = await _service.CreateAsync(t);

        Assert.True(result.Id > 0);
        var inDb = await _db.Transactions.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Equal(250m, inDb.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_ReturnsNull()
    {
        var t = new Transaction
        {
            Amount = 100m, Type = TransactionType.Expense,
            Category = "Food & Dining", Date = DateTime.Today,
            UserId = OtherUserId
        };
        _db.Transactions.Add(t);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(t.Id, TestUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_OwnTransaction_ReturnsTrue()
    {
        var t = new Transaction
        {
            Amount = 50m, Type = TransactionType.Expense,
            Category = "Shopping", Date = DateTime.Today,
            UserId = TestUserId
        };
        _db.Transactions.Add(t);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(t.Id, TestUserId);

        Assert.True(result);
        Assert.Null(await _db.Transactions.FindAsync(t.Id));
    }

    [Fact]
    public async Task DeleteAsync_OtherUsersTransaction_ReturnsFalse()
    {
        var t = new Transaction
        {
            Amount = 50m, Type = TransactionType.Expense,
            Category = "Shopping", Date = DateTime.Today,
            UserId = OtherUserId
        };
        _db.Transactions.Add(t);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(t.Id, TestUserId);

        Assert.False(result);
    }

    // ── CSV Export ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportToCsvAsync_IncludesHeaderAndData()
    {
        await SeedTransactions(
            (100m, TransactionType.Income, "Salary", new DateTime(2024, 3, 1))
        );

        var csv = await _service.ExportToCsvAsync(TestUserId, 2024, 3);
        var text = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("Date,Type,Category,Amount,Notes", text);
        Assert.Contains("Salary", text);
        Assert.Contains("100.00", text);
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyResult_OnlyHasHeader()
    {
        var csv = await _service.ExportToCsvAsync(TestUserId, 2099, 1);
        var text = System.Text.Encoding.UTF8.GetString(csv);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(lines); // only header
    }

    // ── Filters ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionsAsync_FilterByCategory_ReturnsMatchingOnly()
    {
        await SeedTransactions(
            (100m, TransactionType.Expense, "Food & Dining", new DateTime(2024, 3, 1)),
            (200m, TransactionType.Expense, "Housing", new DateTime(2024, 3, 5))
        );

        var results = await _service.GetTransactionsAsync(TestUserId, category: "Housing");

        Assert.Single(results);
        Assert.Equal("Housing", results[0].Category);
    }

    [Fact]
    public async Task GetTransactionsAsync_FilterByType_ReturnsMatchingOnly()
    {
        await SeedTransactions(
            (1000m, TransactionType.Income, "Salary", new DateTime(2024, 3, 1)),
            (300m, TransactionType.Expense, "Food & Dining", new DateTime(2024, 3, 5))
        );

        var results = await _service.GetTransactionsAsync(TestUserId, type: TransactionType.Income);

        Assert.Single(results);
        Assert.Equal(TransactionType.Income, results[0].Type);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SeedTransactions(params (decimal Amount, TransactionType Type, string Category, DateTime Date)[] items)
    {
        foreach (var (amount, type, category, date) in items)
        {
            _db.Transactions.Add(new Transaction
            {
                Amount = amount, Type = type,
                Category = category, Date = date,
                UserId = TestUserId
            });
        }
        await _db.SaveChangesAsync();
    }

    public void Dispose() => _db.Dispose();
}

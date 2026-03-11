using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models;

public enum TransactionType
{
    Income,
    Expense
}

public class Transaction
{
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}

public static class TransactionCategories
{
    public static readonly string[] IncomeCategories =
    [
        "Salary", "Freelance", "Investment", "Gift", "Rental", "Other Income"
    ];

    public static readonly string[] ExpenseCategories =
    [
        "Housing", "Food & Dining", "Transportation", "Utilities",
        "Healthcare", "Entertainment", "Shopping", "Education",
        "Subscriptions", "Personal Care", "Travel", "Other Expense"
    ];

    public static string[] GetAll() =>
        [.. IncomeCategories, .. ExpenseCategories];
}

using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinanceTracker.Pages.Transactions;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITransactionService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ITransactionService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    public List<Transaction> Transactions { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? FilterYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterMonth { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterCategory { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterType { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public string[] Categories { get; set; } = TransactionCategories.GetAll();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User)!;

        TransactionType? type = FilterType switch
        {
            "Income" => TransactionType.Income,
            "Expense" => TransactionType.Expense,
            _ => null
        };

        Transactions = await _service.GetTransactionsAsync(
            userId, FilterYear, FilterMonth, FilterCategory, type);

        TotalIncome = Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        TotalExpenses = Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _service.DeleteAsync(id, userId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        var csvBytes = await _service.ExportToCsvAsync(userId, FilterYear, FilterMonth);
        var fileName = FilterYear.HasValue
            ? $"transactions_{FilterYear}_{FilterMonth:D2}.csv"
            : "transactions_all.csv";
        return File(csvBytes, "text/csv", fileName);
    }
}

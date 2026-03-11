using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinanceTracker.Pages.Transactions;

[Authorize]
public class UpsertModel : PageModel
{
    private readonly ITransactionService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public UpsertModel(ITransactionService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    [BindProperty]
    public Transaction Transaction { get; set; } = new()
    {
        Date = DateTime.Today,
        Type = TransactionType.Expense
    };

    public bool IsEdit => Transaction.Id > 0;
    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id.HasValue)
        {
            var userId = _userManager.GetUserId(User)!;
            var existing = await _service.GetByIdAsync(id.Value, userId);
            if (existing == null) return NotFound();
            Transaction = existing;
        }

        BuildCategoryList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            BuildCategoryList();
            return Page();
        }

        var userId = _userManager.GetUserId(User)!;
        Transaction.UserId = userId;

        if (Transaction.Id == 0)
            await _service.CreateAsync(Transaction);
        else
        {
            // Verify ownership
            var existing = await _service.GetByIdAsync(Transaction.Id, userId);
            if (existing == null) return NotFound();
            await _service.UpdateAsync(Transaction);
        }

        TempData["Success"] = $"Transaction {(Transaction.Id == 0 ? "created" : "updated")} successfully!";
        return RedirectToPage("/Transactions/Index");
    }

    private void BuildCategoryList()
    {
        var incomeGroup = new SelectListGroup { Name = "Income" };
        var expenseGroup = new SelectListGroup { Name = "Expense" };

        CategoryOptions = [
            ..TransactionCategories.IncomeCategories.Select(c => new SelectListItem
            {
                Value = c, Text = c, Group = incomeGroup,
                Selected = Transaction.Category == c
            }),
            ..TransactionCategories.ExpenseCategories.Select(c => new SelectListItem
            {
                Value = c, Text = c, Group = expenseGroup,
                Selected = Transaction.Category == c
            })
        ];
    }
}

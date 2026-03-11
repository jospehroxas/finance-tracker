using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinanceTracker.Pages.Dashboard;

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

    public MonthlySummary Summary { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public int SelectedYear { get; set; } = DateTime.Today.Year;

    [BindProperty(SupportsGet = true)]
    public int SelectedMonth { get; set; } = DateTime.Today.Month;

    public List<int> AvailableYears { get; set; } = [];
    public string[] MonthNames { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User)!;

        Summary = await _service.GetMonthlySummaryAsync(userId, SelectedYear, SelectedMonth);

        var currentYear = DateTime.Today.Year;
        AvailableYears = Enumerable.Range(currentYear - 3, 5).ToList();
        MonthNames =
        [
            "January","February","March","April","May","June",
            "July","August","September","October","November","December"
        ];
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        var csvBytes = await _service.ExportToCsvAsync(userId, SelectedYear, SelectedMonth);
        var fileName = $"transactions_{SelectedYear}_{SelectedMonth:D2}.csv";
        return File(csvBytes, "text/csv", fileName);
    }
}

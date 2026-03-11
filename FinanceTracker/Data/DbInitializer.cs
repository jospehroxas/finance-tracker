using FinanceTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Check if AspNetUsers table exists — if not, wipe and recreate from model
        try
        {
            await context.Users.AnyAsync();
        }
        catch
        {
            await context.Database.EnsureDeletedAsync();
        }

        await context.Database.EnsureCreatedAsync();

        // Seed demo user
        const string demoEmail = "demo@financetracker.com";
        const string demoPassword = "Demo@12345";

        var demoUser = await userManager.FindByEmailAsync(demoEmail);
        if (demoUser == null)
        {
            demoUser = new ApplicationUser
            {
                UserName = demoEmail,
                Email = demoEmail,
                EmailConfirmed = true,
                DisplayName = "Demo User"
            };
            await userManager.CreateAsync(demoUser, demoPassword);
        }

        // Seed transactions if none exist
        if (!await context.Transactions.AnyAsync(t => t.UserId == demoUser.Id))
        {
            var random = new Random(42);
            var transactions = new List<Transaction>();
            var now = DateTime.Today;

            for (int monthOffset = -2; monthOffset <= 0; monthOffset++)
            {
                var baseDate = new DateTime(now.Year, now.Month, 1).AddMonths(monthOffset);

                transactions.Add(new Transaction
                {
                    Amount = 4500m,
                    Type = TransactionType.Income,
                    Category = "Salary",
                    Date = baseDate.AddDays(1),
                    Notes = "Monthly salary",
                    UserId = demoUser.Id
                });

                if (monthOffset != -1)
                {
                    transactions.Add(new Transaction
                    {
                        Amount = 800m + random.Next(0, 400),
                        Type = TransactionType.Income,
                        Category = "Freelance",
                        Date = baseDate.AddDays(10),
                        Notes = "Freelance project payment",
                        UserId = demoUser.Id
                    });
                }

                var expenses = new[]
                {
                    (1200m, "Housing", "Monthly rent"),
                    (350m + random.Next(-50, 100), "Food & Dining", "Groceries and restaurants"),
                    (120m, "Transportation", "Monthly transit pass"),
                    (80m, "Utilities", "Electricity bill"),
                    (60m, "Utilities", "Internet bill"),
                    (45m, "Subscriptions", "Streaming services"),
                    (30m, "Healthcare", "Pharmacy"),
                    (200m + random.Next(-50, 100), "Shopping", "Clothing and household"),
                    (100m + random.Next(-20, 50), "Entertainment", "Movies and dining out"),
                };

                foreach (var (amount, category, notes) in expenses)
                {
                    transactions.Add(new Transaction
                    {
                        Amount = amount,
                        Type = TransactionType.Expense,
                        Category = category,
                        Date = baseDate.AddDays(random.Next(1, 28)),
                        Notes = notes,
                        UserId = demoUser.Id
                    });
                }
            }

            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }
    }
}

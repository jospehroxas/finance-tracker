# 💰 FinanceTracker

A full-stack personal finance tracking web app built with **ASP.NET Core 8**, **Razor Pages**, **Entity Framework Core**, and **SQLite**.

> **Demo login:** `demo@financetracker.com` / `Demo@12345`

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### 1 — Restore packages
```bash
dotnet restore
```

### 2 — Install EF Core tools (choose one)
```bash
# Option A — local tool (recommended, no admin rights needed)
dotnet tool restore

# Option B — global install (once per machine)
dotnet tool install --global dotnet-ef
```
> Verify: `dotnet ef --version`

### 3 — Apply database migrations

Run from the **solution root** (same folder as `FinanceTracker.sln`):
```bash
dotnet ef database update --project FinanceTracker
```

Or from **inside the project folder**:
```bash
cd FinanceTracker
dotnet ef database update
```

### 4 — Run
```bash
dotnet run --project FinanceTracker
```
Open http://localhost:5000 and log in with `demo@financetracker.com` / `Demo@12345`.

### 5 — Test
```bash
dotnet test
```

---

## 🔧 Troubleshooting

| Error | Fix |
|---|---|
| **"Unable to retrieve project metadata"** | Run `dotnet ef` from solution root with `--project FinanceTracker`, or `cd FinanceTracker` first |
| **"dotnet ef not found"** | Run `dotnet tool restore` from solution root |
| **"Database already up to date"** | ✅ You're good — DB is ready |
| Port conflict | `dotnet run --urls "http://localhost:5001"` |

---

## 📁 Project Structure

```
FinanceTracker.sln
├── .config/dotnet-tools.json     ← local dotnet-ef tool
├── FinanceTracker/               ← main web app
│   ├── Models/                   ApplicationUser, Transaction
│   ├── Data/                     DbContext, Migrations, Seeder
│   ├── Services/                 ITransactionService, TransactionService
│   ├── Pages/                    Dashboard, Transactions CRUD
│   └── Program.cs
└── FinanceTracker.Tests/         ← 15 xUnit unit tests
```

---

## 🌐 Deploy to Azure

1. Create Azure App Service (Free tier, .NET 8 runtime)
2. Add GitHub Secrets: `AZURE_APP_NAME` + `AZURE_PUBLISH_PROFILE`
3. Push to `main` — CI/CD in `.github/workflows/ci.yml` handles the rest

---

## 📄 Resume Bullets

- Built full-stack finance app with **ASP.NET Core 8**, Razor Pages, EF Core, SQLite
- Implemented auth with **ASP.NET Core Identity** (hashing, lockout, [Authorize])
- Designed `ITransactionService` with **15 xUnit tests**, 80%+ service coverage
- Deployed to **Azure App Service** via GitHub Actions CI/CD pipeline

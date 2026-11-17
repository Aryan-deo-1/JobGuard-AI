# AI-Based Fake Job Advertisement and Recruitment Fraud Detection Platform (JobGuard AI)

## Overview
**JobGuard AI** is an enterprise-grade threat intelligence and recruitment security platform built with ASP.NET Core MVC and .NET 8. It identifies fake job postings, flags advance-fee recruitment scams, detects domain impersonation, and provides actionable safety advisories to protect job seekers.

## Technology Stack
- **Framework:** ASP.NET Core MVC (.NET 8)
- **Language:** C#
- **Frontend:** Razor Views (.cshtml), HTML5, CSS3, Bootstrap 5 (v5.3.3), JavaScript
- **Database:** Microsoft SQL Server (LocalDB `MSSQLLocalDB` / SQL Server Express)
- **ORM:** Entity Framework Core 8.0.11 (`Microsoft.EntityFrameworkCore.SqlServer`)
- **Authentication & Authorization:** ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.11) with Role-based Access Control (`Admin`, `User`)
- **Dataset & Machine Learning:** Real or Fake Job Posting Prediction (EMSCAD benchmark) prepared for ML.NET (binary classification)

## Design System & Theme
Predominantly black cybersecurity visual architecture with golden-yellow accents:
- **Main Background:** `#090909` (Dominant 85% Black & Dark Grey)
- **Secondary Background:** `#141414`
- **Card Background:** `#1C1C1C`
- **Golden Yellow Accent:** `#DDBB42` (10% highlights, buttons, key headings)
- **Golden Hover:** `#F2CF55`
- **Primary Text:** `#FFFFFF`
- **Secondary Text:** `#B8B8B8`
- **Borders:** `#333333`
- **Status Colors:** Red `#FF4D4F` (Fraud / High Risk), Green `#52C41A` (Genuine / Verified)

## Project Architecture
- `Controllers/`
  - `HomeController.cs` (Index, Analyze, Result, History, About, Privacy)
  - `AccountController.cs` (Register, Login, Logout, AccessDenied)
- `Data/`
  - `ApplicationDbContext.cs` (Identity-extended DbContext managing `JobAnalyses` and Identity tables)
  - `DbInitializer.cs` (Startup role seeding for `Admin` and `User` roles)
- `Models/`
  - `ApplicationUser.cs` (Custom Identity user extending `IdentityUser` with `FullName`)
  - `JobAnalysis.cs` (Database entity with `UserId` foreign key relation to `ApplicationUser`)
  - `ErrorViewModel.cs`
- `Services/`
  - `IJobAnalysisService.cs` (Persistence, single-record lookup, and user-isolated query contracts)
  - `JobAnalysisService.cs` (EF Core implementation enforcing user record isolation)
- `ViewModels/`
  - `RegisterViewModel.cs` (Registration binding and security validations)
  - `LoginViewModel.cs` (Authentication binding, ReturnUrl, Remember Me)
  - `JobAnalysisViewModel.cs` (Job analyzer form input binding & validation)
  - `PredictionResultViewModel.cs` (Risk scoring & threat reporting presentation)
- `Views/`
  - `Account/Register.cshtml` (Black & Gold user registration interface)
  - `Account/Login.cshtml` (Secure user sign-in interface)
  - `Account/AccessDenied.cshtml` (403 Forbidden alert explaining ownership restriction)
  - `Home/Index.cshtml` (Homepage with Hero, Features, Workflow, and Advantages)
  - `Home/Analyze.cshtml` (Protected job analyzer form with quick sample pre-fills)
  - `Home/Result.cshtml` (Ownership-verified threat assessment visualizer)
  - `Home/History.cshtml` (User-isolated analysis history list)
  - `Home/About.cshtml` (Educational threat intelligence overview)
  - `Home/Privacy.cshtml` (Zero-knowledge & data privacy commitments)
  - `Shared/_Layout.cshtml` (Dynamic authenticated/anonymous navigation header and cybersecurity footer)
- `Dataset/`
  - `Raw/`
    - `fake_job_postings.csv` (Original authentic EMSCAD dataset, 17,880 rows, 18 columns, 50MB)
    - `README.md` (Dataset provenance, attribution, and license info)
  - `Processed/`
    - `jobguard_clean.csv` (17,599 deduplicated, cleaned, normalized records)
    - `train.csv` (14,079 rows, 80% stratified training partition)
    - `test.csv` (3,520 rows, 20% stratified testing partition)
  - `Reports/`
    - `DATASET_AUDIT_REPORT.md` (Comprehensive missing-value, duplicate, class imbalance, and leakage analysis)
  - `preprocess.py` (Reproducible cleaning, text normalization, and stratified partitioning script)
- `Migrations/`
  - `20261115085724_InitialCreate.cs` (Base schema creating `JobAnalyses` table and indexes)
  - `20261115091155_AddIdentitySchema.cs` (Identity schema creating `AspNetUsers`, `AspNetRoles`, etc.)
- `wwwroot/`
  - `css/site.css` (Custom theme tokens, cyber cards, badges, and responsive utilities)
  - `js/site.js` (Client-side sample loaders and interactive UI behaviors)
- `MLModels/`
  - `JobGuardModel.zip` (Trained FastTree binary classification model)
  - `JobPostingModelInput.cs` & `JobPostingPrediction.cs` (ML.NET schema models)
  - `ModelTrainer.cs` (Reproducible ML.NET training and evaluation pipeline)
  - `TestSetEvaluator.cs` (Held-out test set evaluator & threshold analysis)
  - `DatabaseAuditor.cs` (Database & referential integrity audit tool)
- `JobGuard.Tests/`
  - xUnit test suite (25 automated unit, regression, scenario, and security tests)

## Dataset & Machine Learning
- **Raw Observations:** 17,880 records (17,014 genuine, 866 fraudulent).
- **Cleaned Observations:** 17,599 records (281 exact feature duplicates removed).
- **Target Label:** `fraudulent` (0 = Genuine, 1 = Fraudulent; 4.86% fraud imbalance).
- **Stratified Partitioning:** 80% Train (`14,079` records) / 20% Test (`3,520` records) with random seed `42`.
- **Model Architecture:** FastTree (Gradient Boosted Trees) trained with ML.NET 3.0.1.
- **Evaluation Performance (`test.csv` - 3,520 records):**
  - **Accuracy:** 98.69%
  - **Precision:** 96.30%
  - **Recall:** 76.02% (80.12% at 0.30 threshold)
  - **F1 Score:** 84.97% (87.54% at 0.30 threshold)
  - **AUC-ROC:** 0.9943
  - **AUC-PR:** 0.9385

## Security Hardening
- **Authentication:** ASP.NET Core Identity with minimum 8-character password policy.
- **Brute-Force Protection:** Account lockout after 5 consecutive failed logins for 15 minutes (`lockoutOnFailure: true`).
- **Cookie Security:** `HttpOnly = true`, `SameSite = SameSiteMode.Lax`.
- **Rate Limiting:** Built-in fixed-window limiter (`auth-limit`: 15 req/min, `analyze-limit`: 30 req/min).
- **Injection Defenses:** Full EF Core parameterized queries, Razor automatic HTML encoding, URL protocol sanitization.
- **HTTP Security Headers:** CSP, X-Frame-Options (`SAMEORIGIN`), X-Content-Type-Options (`nosniff`), Referrer-Policy, Permissions-Policy.
- **IDOR Protection:** User-isolated query enforcement (`WHERE Id = id AND UserId = currentUserId`).

## Getting Started

### Restore & Build
```bash
dotnet restore
dotnet build
```

### Apply Database Migrations
```bash
dotnet tool run dotnet-ef database update
```

### Run Automated Tests
```bash
dotnet test "JobGuard.Tests\JobGuard.Tests.csproj"
```

### Run Model Evaluation or Database Audit
```bash
# Evaluate model on test.csv
dotnet run -- --evaluate-model

# Audit SQL Server database records and integrity
dotnet run -- --audit-db
```

### Run Locally
```bash
dotnet run
```
Navigate to `http://localhost:5000` (or your launch profile port).

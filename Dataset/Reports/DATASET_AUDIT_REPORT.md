# JobGuard AI — Dataset Audit & Preprocessing Report

**Report Generated:** 2026-11-15  
**Project:** JobGuard AI (Fake Job Advertisement & Recruitment Fraud Detection Platform)  
**Task Phase:** Phase 5 (Dataset Collection & Preprocessing)

---

## 1. Executive Summary

This report documents the acquisition, rigorous audit, deduplication, cleaning, and reproducible stratified partitioning of the authentic **Employment Scam Aegean Dataset (EMSCAD)** / **Kaggle Real or Fake Job Posting Prediction** dataset.

The resulting preprocessed datasets in `Dataset/Processed/` are formatted for direct consumption by **ML.NET** binary classification algorithms in Phase 6 without data leakage.

| Metric | Raw Dataset | Processed Dataset | Training Split (80%) | Testing Split (20%) |
| :--- | :--- | :--- | :--- | :--- |
| **Total Records** | **17,880** | **17,599** | **14,079** | **3,520** |
| **Legitimate (0)** | 17,014 (95.16%) | 16,743 (95.14%) | 13,394 (95.13%) | 3,349 (95.14%) |
| **Fraudulent (1)** | 866 (4.84%) | 856 (4.86%) | 685 (4.87%) | 171 (4.86%) |
| **Imbalance Ratio**| 19.6 : 1 | 19.6 : 1 | 19.6 : 1 | 19.6 : 1 |
| **Columns** | 18 | 19 (added `combined_text`) | 19 | 19 |

---

## 2. Dataset Provenance & Attribution

- **Dataset Name:** Real or Fake Job Posting Prediction (EMSCAD)
- **Primary Source:** Laboratory of Information & Communication Systems Security (ICBSS), University of the Aegean, Greece.
- **Kaggle Distribution:** [https://www.kaggle.com/datasets/shivamb/real-or-fake-fake-jobposting-prediction](https://www.kaggle.com/datasets/shivamb/real-or-fake-fake-jobposting-prediction)
- **Repository Mirror:** `https://raw.githubusercontent.com/abbylmm/fake_job_posting/main/data/fake_job_postings.csv`
- **File Name:** `Dataset/Raw/fake_job_postings.csv` (Size: 50,043,721 bytes)
- **License / Terms:** Public research and educational benchmark dataset.

---

## 3. Raw Dataset Column & Missing Value Analysis

The original raw dataset comprises **17,880** records across **18** columns.

| `job_id` | `int64` | 0 | 0.00% |
| `title` | `object` | 0 | 0.00% |
| `location` | `object` | 346 | 1.94% |
| `department` | `object` | 11,547 | 64.58% |
| `salary_range` | `object` | 15,012 | 83.96% |
| `company_profile` | `object` | 3,308 | 18.50% |
| `description` | `object` | 1 | 0.01% |
| `requirements` | `object` | 2,696 | 15.08% |
| `benefits` | `object` | 7,212 | 40.34% |
| `telecommuting` | `int64` | 0 | 0.00% |
| `has_company_logo` | `int64` | 0 | 0.00% |
| `has_questions` | `int64` | 0 | 0.00% |
| `employment_type` | `object` | 3,471 | 19.41% |
| `required_experience` | `object` | 7,050 | 39.43% |
| `required_education` | `object` | 8,105 | 45.33% |
| `industry` | `object` | 4,903 | 27.42% |
| `function` | `object` | 6,455 | 36.10% |
| `fraudulent` | `int64` | 0 | 0.00% |

### Key Observations:
1. **Severe Missingness in Optional Fields:** `salary_range` is absent in 83.96% of listings, and `department` in 64.58%.
2. **Key Feature Availability:** `title`, `job_id`, `telecommuting`, `has_company_logo`, `has_questions`, and the target `fraudulent` have 100% data completeness (0 missing values).
3. **Core Text Coverage:** `description` is present in 99.99% of records (only 1 single listing missing description), while `requirements` (84.92%) and `company_profile` (81.50%) offer substantial textual signal for NLP models.

---

## 4. Duplicate & Redundancy Audit

- **Exact Row Duplicates (including `job_id`):** 0
- **Exact Feature Duplicates (identical across all 17 features except `job_id`):** **281** records.
  - Genuine duplicates dropped: **271**
  - Fraudulent duplicates dropped: **10**
- **Action Taken:** The 281 exact feature duplicates were dropped to prevent redundant weighting and train/test leakage.
- **Content Duplicates Check:** Postings sharing identical `title`, `company_profile`, and `description` were examined for label consistency: **0 conflicting labels** were found across duplicate content, confirming ground-truth integrity.

---

## 5. Cleaning & Preprocessing Operations

1. **HTML Stripping & Entity Decoding:**
   - Applied regex `<[^>]+>` removal and `html.unescape` across all narrative fields (`title`, `company_profile`, `description`, `requirements`, `benefits`).
2. **Whitespace & Carriage Return Normalization:**
   - Multi-line breaks (`\r`, `\n`, `\t`) and redundant spaces were collapsed into clean single spaces. This ensures standard single-line CSV format and prevents line-breaking errors in ML.NET's `LoadFromTextFile`.
3. **Missing Value Imputation Strategy:**
   - Text fields (`company_profile`, `requirements`, `benefits`): Replaced missing values with empty strings `""` to allow clean concatenation.
   - Categorical fields (`location`, `department`, `salary_range`, `employment_type`, `required_experience`, `required_education`, `industry`, `function`): Replaced missing values with `"Unspecified"` to preserve categorical indicator tokens.
   - Binary fields (`telecommuting`, `has_company_logo`, `has_questions`, `fraudulent`): Cast explicitly to integer flags (0 or 1).
4. **Feature Synthesis (`combined_text`):**
   - Synthesized a unified text representation combining all 5 narrative components:
     `title` + " " + `company_profile` + " " + `description` + " " + `requirements` + " " + `benefits`.
   - This provides a high-density textual document ideal for ML.NET `FeaturizeText` and TF-IDF / n-gram tokenization.

---

## 6. Data Leakage Prevention & Stratified Partitioning

To ensure scientific validity and avoid data leakage:
1. **Target Feature Isolation:** `fraudulent` is exclusively retained as the supervised label. No target-derived metrics or leakage proxies were created.
2. **Strict Stratified Split:**
   - An **80.0% / 20.0%** stratified split was executed using a fixed random seed (`random_state=42`).
   - Training partition (`Dataset/Processed/train.csv`): **14,079** rows (13,394 genuine, 685 fraudulent).
   - Testing partition (`Dataset/Processed/test.csv`): **3,520** rows (3,349 genuine, 171 fraudulent).
3. **Partition Independence:**
   - Checked intersection of `job_id` sets between `train.csv` and `test.csv`: **0 overlapping records**.
   - The test partition remains completely untouched and isolated for unbiased evaluation in Phase 6.

---

## 7. ML.NET Schema Specification

The processed files conform to the following schema for ML.NET `IDataView` loading:

```csharp
public class ModelInput
{
    [LoadColumn(0)]
    public float JobId { get; set; }

    [LoadColumn(1)]
    public string Title { get; set; } = string.Empty;

    [LoadColumn(2)]
    public string CompanyProfile { get; set; } = string.Empty;

    [LoadColumn(3)]
    public string Description { get; set; } = string.Empty;

    [LoadColumn(4)]
    public string Requirements { get; set; } = string.Empty;

    [LoadColumn(5)]
    public string Benefits { get; set; } = string.Empty;

    [LoadColumn(6)]
    public string CombinedText { get; set; } = string.Empty;

    [LoadColumn(7)]
    public string Location { get; set; } = string.Empty;

    [LoadColumn(8)]
    public string Department { get; set; } = string.Empty;

    [LoadColumn(9)]
    public string SalaryRange { get; set; } = string.Empty;

    [LoadColumn(10)]
    public string EmploymentType { get; set; } = string.Empty;

    [LoadColumn(11)]
    public string RequiredExperience { get; set; } = string.Empty;

    [LoadColumn(12)]
    public string RequiredEducation { get; set; } = string.Empty;

    [LoadColumn(13)]
    public string Industry { get; set; } = string.Empty;

    [LoadColumn(14)]
    public string Function { get; set; } = string.Empty;

    [LoadColumn(15)]
    public float Telecommuting { get; set; }

    [LoadColumn(16)]
    public float HasCompanyLogo { get; set; }

    [LoadColumn(17)]
    public float HasQuestions { get; set; }

    [LoadColumn(18)]
    public bool Label { get; set; } // Mapped from 'fraudulent' (0 -> false, 1 -> true)
}
```

---

## 8. File Inventory & Storage Summary

- `Dataset/Raw/fake_job_postings.csv` (50,043,721 bytes) — Unmodified source dataset.
- `Dataset/Raw/README.md` — Provenance and attribution metadata.
- `Dataset/Processed/jobguard_clean.csv` (96,058,711 bytes) — Complete deduplicated, cleaned dataset (17,599 records).
- `Dataset/Processed/train.csv` (76,882,930 bytes) — 80% stratified training partition (14,079 records).
- `Dataset/Processed/test.csv` (19,176,021 bytes) — 20% stratified testing partition (3,520 records).
- `Dataset/Reports/DATASET_AUDIT_REPORT.md` — This comprehensive audit report.

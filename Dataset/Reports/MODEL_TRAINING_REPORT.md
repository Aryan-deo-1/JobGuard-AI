# JobGuard AI — ML.NET Model Training & Evaluation Report

**Report Date:** 2026-11-15 09:58:43 UTC  
**Project:** JobGuard AI (AI-Based Fake Job Advertisement & Recruitment Fraud Detection Platform)  
**Target Framework:** .NET 8 (net8.0)  
**ML Framework:** ML.NET 3.0.1 (Microsoft.ML, Microsoft.ML.FastTree)  
**Evaluation Dataset:** EMSCAD Preprocessed Stratified Test Partition (`Dataset/Processed/test.csv`)  

---

## 1. Executive Summary

This report documents the rigorous training, hyperparameter configuration, empirical evaluation, algorithmic comparison, and persistence of machine learning models for detecting fraudulent job advertisements in **JobGuard AI**.

Three supervised binary classification algorithms were evaluated on the untouched 20% test partition (3,520 records) using the EMSCAD benchmark dataset (17,599 total records):
1. **Model A:** Logistic Regression (L-BFGS)
2. **Model B:** FastTree (Gradient Boosted Decision Trees)
3. **Model C:** SDCA Logistic Regression (Stochastic Dual Coordinate Ascent)

Based on a balanced evaluation under severe class imbalance (4.86% fraudulent base rate), **Model B: FastTree (Gradient Boosted Trees)** achieved the superior fraud detection performance with an **F1-Score of 84.97%**, **Recall of 76.02%**, and **AUC-ROC of 0.9943**.

---

## 2. Dataset Partitioning & Class Distribution

| Partition | Source File | Total Postings | Genuine (0) | Fraudulent (1) | Fraud Rate (%) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Training (80%)** | `Dataset/Processed/train.csv` | 14,079 | 13,394 | 685 | 4.866% |
| **Testing (20%)** | `Dataset/Processed/test.csv` | 3,520 | 3,349 | 171 | 4.858% |
| **Total Benchmark** | `Dataset/Processed/jobguard_clean.csv` | 17,599 | 16,743 | 856 | 4.864% |

> [!IMPORTANT]
> **Data Leakage Mitigation:** The unique identifier `job_id` was strictly excluded from all training estimators and feature vectors. All narrative text was tokenized using the unified `combined_text` representation.

---

## 3. Feature Engineering Pipeline

The ML.NET feature engineering pipeline transforms raw heterogeneous tabular and textual signals into a unified dense numeric feature vector:

```csharp
// 1. Map target label
Transforms.CopyColumns("Label", nameof(JobPostingModelInput.Fraudulent))

// 2. Text Featurization (Word & Char N-Grams, TF-IDF, Normalization)
Transforms.Text.FeaturizeText("TextFeatures", nameof(JobPostingModelInput.CombinedText))

// 3. Categorical One-Hot Encoding
Transforms.Categorical.OneHotEncoding(new[] {
    new InputOutputColumnPair("EmploymentTypeEncoded", nameof(JobPostingModelInput.EmploymentType)),
    new InputOutputColumnPair("RequiredExperienceEncoded", nameof(JobPostingModelInput.RequiredExperience)),
    new InputOutputColumnPair("RequiredEducationEncoded", nameof(JobPostingModelInput.RequiredEducation)),
    new InputOutputColumnPair("IndustryEncoded", nameof(JobPostingModelInput.Industry)),
    new InputOutputColumnPair("FunctionEncoded", nameof(JobPostingModelInput.Function))
})

// 4. Feature Vector Concatenation
Transforms.Concatenate("Features",
    "TextFeatures", "EmploymentTypeEncoded", "RequiredExperienceEncoded",
    "RequiredEducationEncoded", "IndustryEncoded", "FunctionEncoded",
    nameof(JobPostingModelInput.Telecommuting),
    nameof(JobPostingModelInput.HasCompanyLogo),
    nameof(JobPostingModelInput.HasQuestions))
```

---

## 4. Algorithmic Comparison & Evaluation Results

All models were fitted on the 14,079 training records and scored against the untouched 3,520 test records.

| Model | Algorithm | Accuracy | Precision (Fraud) | Recall (Fraud) | F1-Score | AUC-ROC | AUC-PR | Train Time |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Model A: Logistic Regression (L-BFGS)** | `LbfgsLogisticRegression` | 96.45 % | 88.33 % | 30.99 % | **45.89 %** | 0.9340 | 0.6364 | 13.63s |
| **Model B: FastTree (Gradient Boosted Trees)** **(Selected)** | `FastTreeBinaryTrainer` | 98.69 % | 96.30 % | 76.02 % | **84.97 %** | 0.9943 | 0.9385 | 64.87s |
| **Model C: SDCA Logistic Regression** | `SdcaLogisticRegression` | 97.16 % | 86.60 % | 49.12 % | **62.69 %** | 0.9615 | 0.7194 | 66.72s |

---

## 5. Confusion Matrices (Evaluated on 3,520 Test Samples)

The test set contains **3,349 Genuine** and **171 Fraudulent** postings.

### Model A: Logistic Regression (L-BFGS)

| Actual \ Predicted | Predicted Genuine (0) | Predicted Fraudulent (1) | Total |
| :--- | :--- | :--- | :--- |
| **Actual Genuine (0)** | **TN = 3,342** | FP = 7 | 3,349 |
| **Actual Fraudulent (1)** | FN = 118 | **TP = 53** | 171 |
| **Total** | 3,460 | 60 | 3,520 |

### Model B: FastTree (Gradient Boosted Trees)

| Actual \ Predicted | Predicted Genuine (0) | Predicted Fraudulent (1) | Total |
| :--- | :--- | :--- | :--- |
| **Actual Genuine (0)** | **TN = 3,344** | FP = 5 | 3,349 |
| **Actual Fraudulent (1)** | FN = 41 | **TP = 130** | 171 |
| **Total** | 3,385 | 135 | 3,520 |

### Model C: SDCA Logistic Regression

| Actual \ Predicted | Predicted Genuine (0) | Predicted Fraudulent (1) | Total |
| :--- | :--- | :--- | :--- |
| **Actual Genuine (0)** | **TN = 3,336** | FP = 13 | 3,349 |
| **Actual Fraudulent (1)** | FN = 87 | **TP = 84** | 171 |
| **Total** | 3,423 | 97 | 3,520 |

---

## 6. Model Selection Justification

### Selected Production Model: **Model B: FastTree (Gradient Boosted Trees)**

In fraud detection systems operating under severe class imbalance (19.6 : 1), raw accuracy is a misleading metric (a trivial dummy classifier predicting 100% genuine achieves 95.14% accuracy while catching zero scams).

1. **Superior Balance (F1-Score: 84.97%):** Model B: FastTree (Gradient Boosted Trees) achieved the highest harmonic mean of precision and recall.
2. **Fraud Detection Sensitivity (Recall: 76.02%):** Identified 130 out of 171 fraudulent postings in the unseen test set.
3. **Discriminative Capacity (AUC-ROC: 0.9943, AUC-PR: 0.9385):** Exceptional separation between legitimate corporate recruitment copy and scam patterns.

---

## 7. Deep-Dive: False Negatives in Recruitment Fraud Detection

In the test set evaluation, 41 fraudulent postings were misclassified as legitimate (False Negatives).

### Why False Negatives Occur in Recruitment Fraud:
1. **Sophisticated Copycat Listings:** Sophisticated fraudsters scrape and copy verbatim descriptions from authentic Fortune 500 job listings, altering only private contact emails or routing links.
2. **Sparse Text Listings:** Postings with minimal descriptive text (fewer than 20 words) lack sufficient n-gram tokens for lexical classifiers to discern malicious intent.
3. **Realistic Categorical Features:** Scammers frequently populate standard employment parameters (`Full-time`, `Mid-Senior level`, `Bachelor's Degree`) to mimic legitimate corporate listings.

### Real-World Implications & Mitigation:
- A **False Negative** represents a direct user safety risk: a job seeker may be deceived into sending personal identity documents or wire fees.
- A **False Positive** represents an operational friction: a legitimate job posting is flagged for administrative review.
- **JobGuard AI Architecture Defense:** In Phase 7 and future phases, JobGuard AI pairs this machine learning model with heuristic rules (email domain validation, wire payment keywords, anonymous messaging flags) to catch sophisticated edge cases.

---

## 8. Persisted Artifacts & Verification

- **Saved Model File:** `MLModels/JobGuardModel.zip`
- **File Size:** 26,803,858 bytes (25.56 MB)
- **Format:** ML.NET ZIP package containing serialized model architecture, FeaturizeText n-gram dictionaries, one-hot category maps, and trained weights.

### Inference Engine Verification:
The persisted ZIP was reloaded into a fresh MLContext and verified against two control samples:

```plaintext
Control Sample 1 (Authentic Senior C# Engineer):
  Predicted Label: GENUINE
  Probability:     0.06%
  Score:           -18.7025

Control Sample 2 (Work from Home Urgent Wire Scam):
  Predicted Label: FRAUDULENT
  Probability:     96.87%
  Score:           8.5810
```

---

## 9. How to Reproduce Training

To retrain and re-evaluate all models from the terminal:

```powershell
# Execute training routine
dotnet run -- --train-models
```

This command executes `ModelTrainer.Run()`, fits all 3 algorithms, logs metrics to console, updates `JobGuardModel.zip`, and updates this audit report.

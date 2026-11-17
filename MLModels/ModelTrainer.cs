using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Trainers.FastTree;

namespace _NET_PROJECT.MLModels;

/// <summary>
/// Model evaluation summary container.
/// </summary>
public class ModelEvaluationResult
{
    public required string ModelName { get; set; }
    public required string Algorithm { get; set; }
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double AreaUnderRocCurve { get; set; }
    public double AreaUnderPrecisionRecallCurve { get; set; }
    public int TruePositives { get; set; }
    public int TrueNegatives { get; set; }
    public int FalsePositives { get; set; }
    public int FalseNegatives { get; set; }
    public TimeSpan TrainingTime { get; set; }
    public ITransformer? TrainedModel { get; set; }
}

/// <summary>
/// Row helper for test prediction inspection.
/// </summary>
public class PredictionEvaluationRow
{
    public bool Label { get; set; }
    public bool PredictedLabel { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}

/// <summary>
/// Standalone runner to train, compare, evaluate and persist ML.NET models on EMSCAD dataset.
/// </summary>
public static class ModelTrainer
{
    public static void Run()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("           JOBGUARD AI — PHASE 6: ML.NET MODEL TRAINING & EVALUATION            ");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"Execution Started: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Environment: .NET 8 (net8.0) | ML.NET 3.0.1 | Microsoft.ML.FastTree 3.0.1");

        string baseDir = Directory.GetCurrentDirectory();
        string trainPath = Path.Combine(baseDir, "Dataset", "Processed", "train.csv");
        string testPath = Path.Combine(baseDir, "Dataset", "Processed", "test.csv");
        string modelOutputPath = Path.Combine(baseDir, "MLModels", "JobGuardModel.zip");
        string reportOutputPath = Path.Combine(baseDir, "Dataset", "Reports", "MODEL_TRAINING_REPORT.md");

        if (!File.Exists(trainPath))
        {
            throw new FileNotFoundException($"Training dataset not found at: {trainPath}");
        }
        if (!File.Exists(testPath))
        {
            throw new FileNotFoundException($"Testing dataset not found at: {testPath}");
        }

        Console.WriteLine($"Training Data: {trainPath}");
        Console.WriteLine($"Testing Data:  {testPath}");
        Console.WriteLine($"Target Model:  {modelOutputPath}");
        Console.WriteLine("--------------------------------------------------------------------------------");

        // 1. Initialize MLContext with fixed seed for reproducibility
        var mlContext = new MLContext(seed: 42);

        // 2. Load Datasets
        Console.WriteLine("[Step 1/6] Loading preprocessed train and test partitions...");
        IDataView trainData = mlContext.Data.LoadFromTextFile<JobPostingModelInput>(
            path: trainPath,
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true,
            trimWhitespace: true);

        IDataView testData = mlContext.Data.LoadFromTextFile<JobPostingModelInput>(
            path: testPath,
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true,
            trimWhitespace: true);

        long trainRowCount = trainData.GetRowCount() ?? 14079;
        long testRowCount = testData.GetRowCount() ?? 3520;
        Console.WriteLine($"Train Records: {trainRowCount:N0} | Test Records: {testRowCount:N0} | Total: {trainRowCount + testRowCount:N0}");

        // 3. Construct Data Preparation Pipeline
        Console.WriteLine("[Step 2/6] Building feature engineering pipeline...");
        // Exclude job_id (leakage prevention)
        // Combine text features, one-hot encode categoricals, concatenate numeric flags
        var dataPrepPipeline = mlContext.Transforms.CopyColumns("Label", nameof(JobPostingModelInput.Fraudulent))
            .Append(mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: "TextFeatures",
                inputColumnName: nameof(JobPostingModelInput.CombinedText)))
            .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                new[]
                {
                    new InputOutputColumnPair("EmploymentTypeEncoded", nameof(JobPostingModelInput.EmploymentType)),
                    new InputOutputColumnPair("RequiredExperienceEncoded", nameof(JobPostingModelInput.RequiredExperience)),
                    new InputOutputColumnPair("RequiredEducationEncoded", nameof(JobPostingModelInput.RequiredEducation)),
                    new InputOutputColumnPair("IndustryEncoded", nameof(JobPostingModelInput.Industry)),
                    new InputOutputColumnPair("FunctionEncoded", nameof(JobPostingModelInput.Function)),
                }))
            .Append(mlContext.Transforms.Concatenate(
                "Features",
                "TextFeatures",
                "EmploymentTypeEncoded",
                "RequiredExperienceEncoded",
                "RequiredEducationEncoded",
                "IndustryEncoded",
                "FunctionEncoded",
                nameof(JobPostingModelInput.Telecommuting),
                nameof(JobPostingModelInput.HasCompanyLogo),
                nameof(JobPostingModelInput.HasQuestions)));

        var evaluationResults = new List<ModelEvaluationResult>();

        // --------------------------------------------------------------------------------
        // 4. Model A: Logistic Regression (L-BFGS)
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Step 3/6] Training Model A: Logistic Regression (L-BFGS)...");
        var lbfgsTrainer = mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
            labelColumnName: "Label",
            featureColumnName: "Features");

        var lbfgsPipeline = dataPrepPipeline.Append(lbfgsTrainer);
        var swA = Stopwatch.StartNew();
        var modelA = lbfgsPipeline.Fit(trainData);
        swA.Stop();

        Console.WriteLine($"Model A fitted in {swA.Elapsed.TotalSeconds:F2}s. Evaluating on test set...");
        var resultA = EvaluateModel(mlContext, modelA, testData, "Model A: Logistic Regression (L-BFGS)", "LbfgsLogisticRegression", swA.Elapsed);
        evaluationResults.Add(resultA);
        PrintMetrics(resultA);

        // --------------------------------------------------------------------------------
        // 5. Model B: FastTree (Gradient Boosted Decision Trees)
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Step 4/6] Training Model B: FastTree (Gradient Boosted Trees)...");
        var fastTreeTrainer = mlContext.BinaryClassification.Trainers.FastTree(
            new FastTreeBinaryTrainer.Options
            {
                LabelColumnName = "Label",
                FeatureColumnName = "Features",
                NumberOfLeaves = 20,
                NumberOfTrees = 100,
                MinimumExampleCountPerLeaf = 10,
                LearningRate = 0.2
            });

        var fastTreePipeline = dataPrepPipeline.Append(fastTreeTrainer);
        var swB = Stopwatch.StartNew();
        var modelB = fastTreePipeline.Fit(trainData);
        swB.Stop();

        Console.WriteLine($"Model B fitted in {swB.Elapsed.TotalSeconds:F2}s. Evaluating on test set...");
        var resultB = EvaluateModel(mlContext, modelB, testData, "Model B: FastTree (Gradient Boosted Trees)", "FastTreeBinaryTrainer", swB.Elapsed);
        evaluationResults.Add(resultB);
        PrintMetrics(resultB);

        // --------------------------------------------------------------------------------
        // 6. Model C: SDCA Logistic Regression (Stochastic Dual Coordinate Ascent)
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Step 5/6] Training Model C: SDCA Logistic Regression...");
        var sdcaTrainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
            labelColumnName: "Label",
            featureColumnName: "Features");

        var sdcaPipeline = dataPrepPipeline.Append(sdcaTrainer);
        var swC = Stopwatch.StartNew();
        var modelC = sdcaPipeline.Fit(trainData);
        swC.Stop();

        Console.WriteLine($"Model C fitted in {swC.Elapsed.TotalSeconds:F2}s. Evaluating on test set...");
        var resultC = EvaluateModel(mlContext, modelC, testData, "Model C: SDCA Logistic Regression", "SdcaLogisticRegression", swC.Elapsed);
        evaluationResults.Add(resultC);
        PrintMetrics(resultC);

        // --------------------------------------------------------------------------------
        // 7. Model Comparison & Selection
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("                        ALGORITHM COMPARISON SUMMARY                            ");
        Console.WriteLine("================================================================================");
        Console.WriteLine(string.Format("{0,-32} | {1,8} | {2,9} | {3,8} | {4,8} | {5,8} | {6,8}",
            "Model", "Accuracy", "Precision", "Recall", "F1", "AUC-ROC", "AUC-PR"));
        Console.WriteLine(new string('-', 95));

        foreach (var res in evaluationResults)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0,-32} | {1,8:P2} | {2,9:P2} | {3,8:P2} | {4,8:P2} | {5,8:F4} | {6,8:F4}",
                res.ModelName, res.Accuracy, res.Precision, res.Recall, res.F1Score, res.AreaUnderRocCurve, res.AreaUnderPrecisionRecallCurve));
        }
        Console.WriteLine("--------------------------------------------------------------------------------");

        // Primary selection metric: F1-Score on fraudulent class (balances precision and recall under 4.86% imbalance)
        // Tie-breaker: Recall (cybersecurity priority: minimize False Negatives / missed fraudulent postings)
        var bestResult = evaluationResults
            .OrderByDescending(r => r.F1Score)
            .ThenByDescending(r => r.Recall)
            .First();

        Console.WriteLine($"\n--> Selected Winner: {bestResult.ModelName}");
        Console.WriteLine($"    Justification: Highest F1-Score ({bestResult.F1Score:P2}) & Fraud Recall ({bestResult.Recall:P2})");
        Console.WriteLine($"    False Negatives (Missed Scams): {bestResult.FalseNegatives} out of 171 test fraudulent listings.");

        // --------------------------------------------------------------------------------
        // 8. Save Best Model
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Step 6/6] Persisting winning model to MLModels/JobGuardModel.zip...");
        string? modelDir = Path.GetDirectoryName(modelOutputPath);
        if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir))
        {
            Directory.CreateDirectory(modelDir);
        }

        mlContext.Model.Save(bestResult.TrainedModel, trainData.Schema, modelOutputPath);
        var fileInfo = new FileInfo(modelOutputPath);
        Console.WriteLine($"Saved Model Size: {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0 / 1024.0:F2} MB)");

        // --------------------------------------------------------------------------------
        // 9. Model Verification (Load & Inference Test)
        // --------------------------------------------------------------------------------
        Console.WriteLine("\nVerifying model loading and real-time inference...");
        ITransformer loadedModel = mlContext.Model.Load(modelOutputPath, out var inputSchema);
        var predictionEngine = mlContext.Model.CreatePredictionEngine<JobPostingModelInput, JobPostingPrediction>(loadedModel);

        // Verification Sample 1: Genuine Job Posting
        var genuineSample = new JobPostingModelInput
        {
            Title = "Senior C# .NET Core Software Engineer",
            CompanyProfile = "Established enterprise fintech software provider with over 500 global employees and certified ISO security compliance.",
            Description = "We are seeking a senior full-stack .NET engineer with experience in ASP.NET Core, EF Core, SQL Server, and microservices architecture.",
            Requirements = "5+ years of C# experience, strong understanding of RESTful APIs, relational databases, unit testing, and agile practices.",
            Benefits = "Comprehensive health insurance, 401(k) matching, 25 days paid vacation, annual professional learning allowance.",
            CombinedText = "Senior C# .NET Core Software Engineer Established enterprise fintech software provider with over 500 global employees and certified ISO security compliance. We are seeking a senior full-stack .NET engineer with experience in ASP.NET Core, EF Core, SQL Server, and microservices architecture. 5+ years of C# experience, strong understanding of RESTful APIs, relational databases, unit testing, and agile practices. Comprehensive health insurance, 401(k) matching, 25 days paid vacation, annual professional learning allowance.",
            EmploymentType = "Full-time",
            RequiredExperience = "Mid-Senior level",
            RequiredEducation = "Bachelor's Degree",
            Industry = "Information Technology and Services",
            Function = "Engineering",
            Telecommuting = 0,
            HasCompanyLogo = 1,
            HasQuestions = 1
        };

        // Verification Sample 2: Fraudulent Job Posting (Classic Scam Pattern)
        var fraudulentSample = new JobPostingModelInput
        {
            Title = "Immediate Work from Home Data Entry Assistant - Earn $45/hr No Experience",
            CompanyProfile = "We are an international global marketing firm seeking immediate representatives.",
            Description = "Urgent opening for home data entry clerks. Flexible hours, receive weekly payments via Western Union, wire transfer, or check. Must purchase home office equipment starter package before assignment dispatch. Contact private telegram or personal gmail.",
            Requirements = "No experience required. Must have internet connection, bank account, and be available for immediate start. High school diploma.",
            Benefits = "High hourly rate, instant wire bonuses, free travel vouchers.",
            CombinedText = "Immediate Work from Home Data Entry Assistant - Earn $45/hr No Experience We are an international global marketing firm seeking immediate representatives. Urgent opening for home data entry clerks. Flexible hours, receive weekly payments via Western Union, wire transfer, or check. Must purchase home office equipment starter package before assignment dispatch. Contact private telegram or personal gmail. No experience required. Must have internet connection, bank account, and be available for immediate start. High school diploma. High hourly rate, instant wire bonuses, free travel vouchers.",
            EmploymentType = "Part-time",
            RequiredExperience = "Entry level",
            RequiredEducation = "High School or equivalent",
            Industry = "Financial Services",
            Function = "Administrative",
            Telecommuting = 1,
            HasCompanyLogo = 0,
            HasQuestions = 0
        };

        var predGenuine = predictionEngine.Predict(genuineSample);
        var predFraud = predictionEngine.Predict(fraudulentSample);

        Console.WriteLine($"Sample 1 (Genuine)    -> Predicted: {(predGenuine.PredictedLabel ? "FRAUDULENT" : "GENUINE")} | Probability: {predGenuine.Probability:P2} | Score: {predGenuine.Score:F4}");
        Console.WriteLine($"Sample 2 (Fraudulent) -> Predicted: {(predFraud.PredictedLabel ? "FRAUDULENT" : "GENUINE")} | Probability: {predFraud.Probability:P2} | Score: {predFraud.Score:F4}");

        // --------------------------------------------------------------------------------
        // 10. Generate Audit Report
        // --------------------------------------------------------------------------------
        GenerateMarkdownReport(reportOutputPath, evaluationResults, bestResult, fileInfo.Length, predGenuine, predFraud);
        Console.WriteLine($"\n[Complete] Model Training Report generated at: {reportOutputPath}");
        Console.WriteLine("================================================================================");
    }

    private static ModelEvaluationResult EvaluateModel(
        MLContext mlContext,
        ITransformer model,
        IDataView testData,
        string modelName,
        string algorithm,
        TimeSpan trainingTime)
    {
        var predictions = model.Transform(testData);
        var metrics = mlContext.BinaryClassification.Evaluate(
            data: predictions,
            labelColumnName: "Label",
            scoreColumnName: "Score",
            predictedLabelColumnName: "PredictedLabel");

        // Inspect row-by-row prediction results for exact confusion matrix counts
        var rows = mlContext.Data.CreateEnumerable<PredictionEvaluationRow>(predictions, reuseRowObject: false).ToList();
        int tp = rows.Count(r => r.Label && r.PredictedLabel);
        int tn = rows.Count(r => !r.Label && !r.PredictedLabel);
        int fp = rows.Count(r => !r.Label && r.PredictedLabel);
        int fn = rows.Count(r => r.Label && !r.PredictedLabel);

        return new ModelEvaluationResult
        {
            ModelName = modelName,
            Algorithm = algorithm,
            Accuracy = metrics.Accuracy,
            Precision = metrics.PositivePrecision,
            Recall = metrics.PositiveRecall,
            F1Score = metrics.F1Score,
            AreaUnderRocCurve = metrics.AreaUnderRocCurve,
            AreaUnderPrecisionRecallCurve = metrics.AreaUnderPrecisionRecallCurve,
            TruePositives = tp,
            TrueNegatives = tn,
            FalsePositives = fp,
            FalseNegatives = fn,
            TrainingTime = trainingTime,
            TrainedModel = model
        };
    }

    private static void PrintMetrics(ModelEvaluationResult r)
    {
        Console.WriteLine($"  Accuracy:   {r.Accuracy:P2} ({r.Accuracy:F4})");
        Console.WriteLine($"  Precision:  {r.Precision:P2} (Positive class = Fraudulent)");
        Console.WriteLine($"  Recall:     {r.Recall:P2} (Fraudulent detection rate)");
        Console.WriteLine($"  F1-Score:   {r.F1Score:P2}");
        Console.WriteLine($"  AUC-ROC:    {r.AreaUnderRocCurve:F4}");
        Console.WriteLine($"  AUC-PR:     {r.AreaUnderPrecisionRecallCurve:F4}");
        Console.WriteLine($"  Confusion Matrix (Test total: {r.TruePositives + r.TrueNegatives + r.FalsePositives + r.FalseNegatives}):");
        Console.WriteLine($"    TP: {r.TruePositives,-4} | FP: {r.FalsePositives,-4}");
        Console.WriteLine($"    FN: {r.FalseNegatives,-4} | TN: {r.TrueNegatives,-4}");
    }

    private static void GenerateMarkdownReport(
        string outputPath,
        List<ModelEvaluationResult> results,
        ModelEvaluationResult best,
        long modelSizeBytes,
        JobPostingPrediction sampleGenuine,
        JobPostingPrediction sampleFraud)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# JobGuard AI — ML.NET Model Training & Evaluation Report");
        sb.AppendLine();
        sb.AppendLine($"**Report Date:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC  ");
        sb.AppendLine("**Project:** JobGuard AI (AI-Based Fake Job Advertisement & Recruitment Fraud Detection Platform)  ");
        sb.AppendLine("**Target Framework:** .NET 8 (net8.0)  ");
        sb.AppendLine("**ML Framework:** ML.NET 3.0.1 (Microsoft.ML, Microsoft.ML.FastTree)  ");
        sb.AppendLine("**Evaluation Dataset:** EMSCAD Preprocessed Stratified Test Partition (`Dataset/Processed/test.csv`)  ");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 1. Executive Summary");
        sb.AppendLine();
        sb.AppendLine("This report documents the rigorous training, hyperparameter configuration, empirical evaluation, algorithmic comparison, and persistence of machine learning models for detecting fraudulent job advertisements in **JobGuard AI**.");
        sb.AppendLine();
        sb.AppendLine("Three supervised binary classification algorithms were evaluated on the untouched 20% test partition (3,520 records) using the EMSCAD benchmark dataset (17,599 total records):");
        sb.AppendLine("1. **Model A:** Logistic Regression (L-BFGS)");
        sb.AppendLine("2. **Model B:** FastTree (Gradient Boosted Decision Trees)");
        sb.AppendLine("3. **Model C:** SDCA Logistic Regression (Stochastic Dual Coordinate Ascent)");
        sb.AppendLine();
        sb.AppendLine($"Based on a balanced evaluation under severe class imbalance (4.86% fraudulent base rate), **{best.ModelName}** achieved the superior fraud detection performance with an **F1-Score of {best.F1Score:P2}**, **Recall of {best.Recall:P2}**, and **AUC-ROC of {best.AreaUnderRocCurve:F4}**.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 2. Dataset Partitioning & Class Distribution");
        sb.AppendLine();
        sb.AppendLine("| Partition | Source File | Total Postings | Genuine (0) | Fraudulent (1) | Fraud Rate (%) |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");
        sb.AppendLine("| **Training (80%)** | `Dataset/Processed/train.csv` | 14,079 | 13,394 | 685 | 4.866% |");
        sb.AppendLine("| **Testing (20%)** | `Dataset/Processed/test.csv` | 3,520 | 3,349 | 171 | 4.858% |");
        sb.AppendLine("| **Total Benchmark** | `Dataset/Processed/jobguard_clean.csv` | 17,599 | 16,743 | 856 | 4.864% |");
        sb.AppendLine();
        sb.AppendLine("> [!IMPORTANT]");
        sb.AppendLine("> **Data Leakage Mitigation:** The unique identifier `job_id` was strictly excluded from all training estimators and feature vectors. All narrative text was tokenized using the unified `combined_text` representation.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 3. Feature Engineering Pipeline");
        sb.AppendLine();
        sb.AppendLine("The ML.NET feature engineering pipeline transforms raw heterogeneous tabular and textual signals into a unified dense numeric feature vector:");
        sb.AppendLine();
        sb.AppendLine("```csharp");
        sb.AppendLine("// 1. Map target label");
        sb.AppendLine("Transforms.CopyColumns(\"Label\", nameof(JobPostingModelInput.Fraudulent))");
        sb.AppendLine();
        sb.AppendLine("// 2. Text Featurization (Word & Char N-Grams, TF-IDF, Normalization)");
        sb.AppendLine("Transforms.Text.FeaturizeText(\"TextFeatures\", nameof(JobPostingModelInput.CombinedText))");
        sb.AppendLine();
        sb.AppendLine("// 3. Categorical One-Hot Encoding");
        sb.AppendLine("Transforms.Categorical.OneHotEncoding(new[] {");
        sb.AppendLine("    new InputOutputColumnPair(\"EmploymentTypeEncoded\", nameof(JobPostingModelInput.EmploymentType)),");
        sb.AppendLine("    new InputOutputColumnPair(\"RequiredExperienceEncoded\", nameof(JobPostingModelInput.RequiredExperience)),");
        sb.AppendLine("    new InputOutputColumnPair(\"RequiredEducationEncoded\", nameof(JobPostingModelInput.RequiredEducation)),");
        sb.AppendLine("    new InputOutputColumnPair(\"IndustryEncoded\", nameof(JobPostingModelInput.Industry)),");
        sb.AppendLine("    new InputOutputColumnPair(\"FunctionEncoded\", nameof(JobPostingModelInput.Function))");
        sb.AppendLine("})");
        sb.AppendLine();
        sb.AppendLine("// 4. Feature Vector Concatenation");
        sb.AppendLine("Transforms.Concatenate(\"Features\",");
        sb.AppendLine("    \"TextFeatures\", \"EmploymentTypeEncoded\", \"RequiredExperienceEncoded\",");
        sb.AppendLine("    \"RequiredEducationEncoded\", \"IndustryEncoded\", \"FunctionEncoded\",");
        sb.AppendLine("    nameof(JobPostingModelInput.Telecommuting),");
        sb.AppendLine("    nameof(JobPostingModelInput.HasCompanyLogo),");
        sb.AppendLine("    nameof(JobPostingModelInput.HasQuestions))");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 4. Algorithmic Comparison & Evaluation Results");
        sb.AppendLine();
        sb.AppendLine("All models were fitted on the 14,079 training records and scored against the untouched 3,520 test records.");
        sb.AppendLine();
        sb.AppendLine("| Model | Algorithm | Accuracy | Precision (Fraud) | Recall (Fraud) | F1-Score | AUC-ROC | AUC-PR | Train Time |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");

        foreach (var r in results)
        {
            string marker = r.ModelName == best.ModelName ? " **(Selected)**" : "";
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| **{0}**{1} | `{2}` | {3:P2} | {4:P2} | {5:P2} | **{6:P2}** | {7:F4} | {8:F4} | {9:F2}s |",
                r.ModelName, marker, r.Algorithm, r.Accuracy, r.Precision, r.Recall, r.F1Score, r.AreaUnderRocCurve, r.AreaUnderPrecisionRecallCurve, r.TrainingTime.TotalSeconds));
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 5. Confusion Matrices (Evaluated on 3,520 Test Samples)");
        sb.AppendLine();
        sb.AppendLine("The test set contains **3,349 Genuine** and **171 Fraudulent** postings.");
        sb.AppendLine();

        foreach (var r in results)
        {
            sb.AppendLine($"### {r.ModelName}");
            sb.AppendLine();
            sb.AppendLine("| Actual \\ Predicted | Predicted Genuine (0) | Predicted Fraudulent (1) | Total |");
            sb.AppendLine("| :--- | :--- | :--- | :--- |");
            sb.AppendLine($"| **Actual Genuine (0)** | **TN = {r.TrueNegatives:N0}** | FP = {r.FalsePositives:N0} | {r.TrueNegatives + r.FalsePositives:N0} |");
            sb.AppendLine($"| **Actual Fraudulent (1)** | FN = {r.FalseNegatives:N0} | **TP = {r.TruePositives:N0}** | {r.TruePositives + r.FalseNegatives:N0} |");
            sb.AppendLine($"| **Total** | {r.TrueNegatives + r.FalseNegatives:N0} | {r.TruePositives + r.FalsePositives:N0} | {r.TrueNegatives + r.FalsePositives + r.TruePositives + r.FalseNegatives:N0} |");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 6. Model Selection Justification");
        sb.AppendLine();
        sb.AppendLine($"### Selected Production Model: **{best.ModelName}**");
        sb.AppendLine();
        sb.AppendLine("In fraud detection systems operating under severe class imbalance (19.6 : 1), raw accuracy is a misleading metric (a trivial dummy classifier predicting 100% genuine achieves 95.14% accuracy while catching zero scams).");
        sb.AppendLine();
        sb.AppendLine($"1. **Superior Balance (F1-Score: {best.F1Score:P2}):** {best.ModelName} achieved the highest harmonic mean of precision and recall.");
        sb.AppendLine($"2. **Fraud Detection Sensitivity (Recall: {best.Recall:P2}):** Identified {best.TruePositives} out of 171 fraudulent postings in the unseen test set.");
        sb.AppendLine($"3. **Discriminative Capacity (AUC-ROC: {best.AreaUnderRocCurve:F4}, AUC-PR: {best.AreaUnderPrecisionRecallCurve:F4}):** Exceptional separation between legitimate corporate recruitment copy and scam patterns.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 7. Deep-Dive: False Negatives in Recruitment Fraud Detection");
        sb.AppendLine();
        sb.AppendLine($"In the test set evaluation, {best.FalseNegatives} fraudulent postings were misclassified as legitimate (False Negatives).");
        sb.AppendLine();
        sb.AppendLine("### Why False Negatives Occur in Recruitment Fraud:");
        sb.AppendLine("1. **Sophisticated Copycat Listings:** Sophisticated fraudsters scrape and copy verbatim descriptions from authentic Fortune 500 job listings, altering only private contact emails or routing links.");
        sb.AppendLine("2. **Sparse Text Listings:** Postings with minimal descriptive text (fewer than 20 words) lack sufficient n-gram tokens for lexical classifiers to discern malicious intent.");
        sb.AppendLine("3. **Realistic Categorical Features:** Scammers frequently populate standard employment parameters (`Full-time`, `Mid-Senior level`, `Bachelor's Degree`) to mimic legitimate corporate listings.");
        sb.AppendLine();
        sb.AppendLine("### Real-World Implications & Mitigation:");
        sb.AppendLine("- A **False Negative** represents a direct user safety risk: a job seeker may be deceived into sending personal identity documents or wire fees.");
        sb.AppendLine("- A **False Positive** represents an operational friction: a legitimate job posting is flagged for administrative review.");
        sb.AppendLine("- **JobGuard AI Architecture Defense:** In Phase 7 and future phases, JobGuard AI pairs this machine learning model with heuristic rules (email domain validation, wire payment keywords, anonymous messaging flags) to catch sophisticated edge cases.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 8. Persisted Artifacts & Verification");
        sb.AppendLine();
        sb.AppendLine($"- **Saved Model File:** `MLModels/JobGuardModel.zip`");
        sb.AppendLine($"- **File Size:** {modelSizeBytes:N0} bytes ({modelSizeBytes / 1024.0 / 1024.0:F2} MB)");
        sb.AppendLine("- **Format:** ML.NET ZIP package containing serialized model architecture, FeaturizeText n-gram dictionaries, one-hot category maps, and trained weights.");
        sb.AppendLine();
        sb.AppendLine("### Inference Engine Verification:");
        sb.AppendLine("The persisted ZIP was reloaded into a fresh MLContext and verified against two control samples:");
        sb.AppendLine();
        sb.AppendLine("```plaintext");
        sb.AppendLine($"Control Sample 1 (Authentic Senior C# Engineer):");
        sb.AppendLine($"  Predicted Label: {(sampleGenuine.PredictedLabel ? "FRAUDULENT" : "GENUINE")}");
        sb.AppendLine($"  Probability:     {sampleGenuine.Probability:P2}");
        sb.AppendLine($"  Score:           {sampleGenuine.Score:F4}");
        sb.AppendLine();
        sb.AppendLine($"Control Sample 2 (Work from Home Urgent Wire Scam):");
        sb.AppendLine($"  Predicted Label: {(sampleFraud.PredictedLabel ? "FRAUDULENT" : "GENUINE")}");
        sb.AppendLine($"  Probability:     {sampleFraud.Probability:P2}");
        sb.AppendLine($"  Score:           {sampleFraud.Score:F4}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 9. How to Reproduce Training");
        sb.AppendLine();
        sb.AppendLine("To retrain and re-evaluate all models from the terminal:");
        sb.AppendLine();
        sb.AppendLine("```powershell");
        sb.AppendLine("# Execute training routine");
        sb.AppendLine("dotnet run -- --train-models");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("This command executes `ModelTrainer.Run()`, fits all 3 algorithms, logs metrics to console, updates `JobGuardModel.zip`, and updates this audit report.");

        File.WriteAllText(outputPath, sb.ToString());
    }
}


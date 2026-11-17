using System.Diagnostics;
using System.Globalization;
using Microsoft.ML;
using Microsoft.ML.Data;
using _NET_PROJECT.MLModels;

namespace _NET_PROJECT.MLModels;

public static class TestSetEvaluator
{
    public static void Run()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("             EVALUATING SAVED FASTTREE MODEL ON TEST.CSV                        ");
        Console.WriteLine("================================================================================");

        var mlContext = new MLContext(seed: 42);
        string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "JobGuardModel.zip");
        string testPath = Path.Combine(Directory.GetCurrentDirectory(), "Dataset", "Processed", "test.csv");

        Console.WriteLine($"Loading model from: {modelPath}");
        var model = mlContext.Model.Load(modelPath, out var schema);

        Console.WriteLine($"Loading test data from: {testPath}");
        var testData = mlContext.Data.LoadFromTextFile<JobPostingModelInput>(
            path: testPath,
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true,
            trimWhitespace: true);

        var predictions = model.Transform(testData);

        var metrics = mlContext.BinaryClassification.Evaluate(
            data: predictions,
            labelColumnName: "Label",
            scoreColumnName: "Score",
            predictedLabelColumnName: "PredictedLabel");

        var rows = mlContext.Data.CreateEnumerable<PredictionEvaluationRow>(predictions, reuseRowObject: false).ToList();
        int tp = rows.Count(r => r.Label && r.PredictedLabel);
        int tn = rows.Count(r => !r.Label && !r.PredictedLabel);
        int fp = rows.Count(r => !r.Label && r.PredictedLabel);
        int fn = rows.Count(r => r.Label && !r.PredictedLabel);

        Console.WriteLine($"Accuracy:   {metrics.Accuracy:P2} ({metrics.Accuracy:F4})");
        Console.WriteLine($"Precision:  {metrics.PositivePrecision:P2} (Positive class = Fraudulent)");
        Console.WriteLine($"Recall:     {metrics.PositiveRecall:P2} (Fraudulent detection rate)");
        Console.WriteLine($"F1-Score:   {metrics.F1Score:P2}");
        Console.WriteLine($"AUC-ROC:    {metrics.AreaUnderRocCurve:F4}");
        Console.WriteLine($"AUC-PR:     {metrics.AreaUnderPrecisionRecallCurve:F4}");
        Console.WriteLine($"Confusion Matrix (Test Total: {tp + tn + fp + fn}):");
        Console.WriteLine($"  TP: {tp,-4} | FP: {fp,-4}");
        Console.WriteLine($"  FN: {fn,-4} | TN: {tn,-4}");

        // Now evaluate at different decision thresholds: 0.30, 0.35, 0.40, 0.45, 0.50
        Console.WriteLine("\n--- THRESHOLD SENSITIVITY ON TEST PARTITION ---");
        Console.WriteLine(string.Format("{0,-10} | {1,-8} | {2,-8} | {3,-8} | {4,-8} | {5,-8} | {6,-8}",
            "Threshold", "TP", "FP", "FN", "TN", "Recall", "Precision"));
        Console.WriteLine(new string('-', 65));

        foreach (var th in new[] { 0.20, 0.25, 0.30, 0.35, 0.40, 0.45, 0.50 })
        {
            int t_tp = rows.Count(r => r.Label && r.Probability >= th);
            int t_tn = rows.Count(r => !r.Label && r.Probability < th);
            int t_fp = rows.Count(r => !r.Label && r.Probability >= th);
            int t_fn = rows.Count(r => r.Label && r.Probability < th);

            double rec = t_tp / (double)(t_tp + t_fn);
            double prec = (t_tp + t_fp) > 0 ? t_tp / (double)(t_tp + t_fp) : 0;
            double f1 = (prec + rec) > 0 ? (2 * prec * rec) / (prec + rec) : 0;

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0,-10:F2} | {1,-8} | {2,-8} | {3,-8} | {4,-8} | {5,-8:P2} | {6,-8:P2}",
                th, t_tp, t_fp, t_fn, t_tn, rec, prec));
        }
    }
}

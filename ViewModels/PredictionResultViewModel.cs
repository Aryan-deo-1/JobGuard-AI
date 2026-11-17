namespace _NET_PROJECT.ViewModels;

public class PredictionResultViewModel
{
    public int? RecordId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string JobLocation { get; set; } = string.Empty;

    public string Status { get; set; } = "Genuine"; // "Fraudulent" or "Genuine"
    public int RiskScore { get; set; } = 0; // 0 to 100
    public string RiskLevel { get; set; } = "Low Risk"; // "Critical Risk", "High Risk", "Moderate Risk", "Low Risk"
    public double PredictionProbability { get; set; } = 0.0; // In percent (0.0 - 100.0)
    public string ModelName { get; set; } = "FastTree";

    public string AnalysisSummary { get; set; } = string.Empty;
    public List<string> WarningIndicators { get; set; } = new();
    public List<string> SafetyRecommendations { get; set; } = new();

    public bool IsDemoPlaceholder { get; set; } = false;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

using Microsoft.ML.Data;

namespace _NET_PROJECT.MLModels;

/// <summary>
/// ML.NET binary classification prediction output schema.
/// </summary>
public class JobPostingPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}


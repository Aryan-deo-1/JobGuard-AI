using _NET_PROJECT.Models;

namespace _NET_PROJECT.Services;

public interface IJobAnalysisService
{
    Task<JobAnalysis> SaveAnalysisAsync(JobAnalysis analysis);
    Task<IEnumerable<JobAnalysis>> GetAllAnalysesAsync(int limit = 50);
    Task<IEnumerable<JobAnalysis>> GetAnalysesByUserIdAsync(string userId, int limit = 50);
    Task<JobAnalysis?> GetAnalysisByIdAsync(int id);
}

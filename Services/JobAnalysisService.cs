using Microsoft.EntityFrameworkCore;
using _NET_PROJECT.Data;
using _NET_PROJECT.Models;

namespace _NET_PROJECT.Services;

public class JobAnalysisService : IJobAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JobAnalysisService> _logger;

    public JobAnalysisService(ApplicationDbContext context, ILogger<JobAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<JobAnalysis> SaveAnalysisAsync(JobAnalysis analysis)
    {
        _context.JobAnalyses.Add(analysis);
        await _context.SaveChangesAsync();
        _logger.LogInformation("JobAnalysis record {Id} created for User {UserId}.", analysis.Id, analysis.UserId ?? "Anonymous");
        return analysis;
    }

    public async Task<IEnumerable<JobAnalysis>> GetAllAnalysesAsync(int limit = 50)
    {
        return await _context.JobAnalyses
            .OrderByDescending(j => j.AnalysisDate)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<JobAnalysis>> GetAnalysesByUserIdAsync(string userId, int limit = 50)
    {
        return await _context.JobAnalyses
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.AnalysisDate)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<JobAnalysis?> GetAnalysisByIdAsync(int id)
    {
        return await _context.JobAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id);
    }
}

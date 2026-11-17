using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using _NET_PROJECT.Services;

namespace JobGuard.Tests;

public class MockWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "_NET_PROJECT";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = string.Empty;
}

public static class TestHelper
{
    public static string GetProjectRootPath()
    {
        // Navigate upwards from bin/Debug/net8.0 to find the folder containing MLModels/JobGuardModel.zip
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "MLModels", "JobGuardModel.zip")))
            {
                return dir;
            }
            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent == null || parent == dir) break;
            dir = parent;
        }

        // Fallback to current working directory
        return Directory.GetCurrentDirectory();
    }

    public static IJobFraudPredictionService CreatePredictionService()
    {
        string rootPath = GetProjectRootPath();
        var env = new MockWebHostEnvironment { ContentRootPath = rootPath };
        var logger = NullLogger<JobFraudPredictionService>.Instance;
        return new JobFraudPredictionService(env, logger);
    }
}

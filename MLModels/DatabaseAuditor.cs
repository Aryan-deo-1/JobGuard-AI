using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using _NET_PROJECT.Data;
using _NET_PROJECT.Models;

namespace _NET_PROJECT.MLModels;

public static class DatabaseAuditor
{
    public static async Task RunAsync(IServiceProvider services)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("             SQL SERVER DATABASE & INTEGRITY AUDIT                              ");
        Console.WriteLine("================================================================================");

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Roles Audit
        var roles = await roleManager.Roles.AsNoTracking().ToListAsync();
        Console.WriteLine($"[Roles] Total: {roles.Count}");
        foreach (var r in roles)
        {
            Console.WriteLine($"  - Role: {r.Name} (Id: {r.Id})");
        }

        // 2. Users Audit
        var users = await userManager.Users.AsNoTracking().ToListAsync();
        Console.WriteLine($"\n[Users] Total: {users.Count}");
        foreach (var u in users)
        {
            var userRoles = await userManager.GetRolesAsync(u);
            Console.WriteLine($"  - User: {u.Email} | Name: {u.FullName} | Id: {u.Id} | Roles: [{string.Join(", ", userRoles)}]");
        }

        // 3. JobAnalyses Audit
        var analyses = await dbContext.JobAnalyses.AsNoTracking().OrderBy(a => a.Id).ToListAsync();
        Console.WriteLine($"\n[JobAnalyses Table] Total Records: {analyses.Count}");
        foreach (var a in analyses)
        {
            Console.WriteLine($"  - Id: {a.Id,-6} | Verdict: {a.Prediction,-10} | Risk: {a.RiskScore,3}% | User: {(a.UserId != null ? a.UserId.Substring(0, 8) + "..." : "NULL"),-12} | Title: {a.JobTitle}");
        }

        // 4. Integrity Checks
        bool anyOrphaned = analyses.Any(a => a.UserId != null && !users.Any(u => u.Id == a.UserId));
        Console.WriteLine($"\n[Integrity Check] Orphaned Analyses without valid AspNetUsers: {anyOrphaned}");
        if (anyOrphaned)
        {
            throw new InvalidOperationException("Integrity violation: Orphaned JobAnalyses found with invalid UserId!");
        }
        Console.WriteLine("[Integrity Check] All records maintain 100% referential integrity.");

        Console.WriteLine("================================================================================");
    }
}

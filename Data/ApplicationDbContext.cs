using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using _NET_PROJECT.Models;

namespace _NET_PROJECT.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobAnalysis> JobAnalyses => Set<JobAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JobAnalysis>(entity =>
        {
            entity.ToTable("JobAnalyses");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.JobTitle)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CompanyName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CompanyWebsite)
                .HasMaxLength(500);

            entity.Property(e => e.JobLocation)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.EmploymentType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.JobDescription)
                .IsRequired();

            entity.Property(e => e.Prediction)
                .HasMaxLength(50);

            entity.Property(e => e.AnalysisDate)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationship to ApplicationUser
            entity.HasOne(e => e.User)
                .WithMany(u => u.JobAnalyses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for high-frequency queries
            entity.HasIndex(e => e.AnalysisDate)
                .HasDatabaseName("IX_JobAnalyses_AnalysisDate");

            entity.HasIndex(e => e.CompanyName)
                .HasDatabaseName("IX_JobAnalyses_CompanyName");

            entity.HasIndex(e => e.Prediction)
                .HasDatabaseName("IX_JobAnalyses_Prediction");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_JobAnalyses_UserId");
        });
    }
}

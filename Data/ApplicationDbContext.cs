using Microsoft.EntityFrameworkCore;

namespace KatalonScheduler.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public required DbSet<Organization> Organizations { get; set; }
    public required DbSet<Project> Projects { get; set; }
    public required DbSet<TestCase> TestCases { get; set; }
    public required DbSet<TestSuite> TestSuites { get; set; }
    public required DbSet<ScheduledTest> ScheduledTests { get; set; }
    public required DbSet<AdminSettings> AdminSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Organization)
            .WithMany(o => o.Projects)
            .HasForeignKey(p => p.OrganizationId);

        modelBuilder.Entity<TestSuite>()
            .HasOne(ts => ts.Project)
            .WithMany(p => p.TestSuites)
            .HasForeignKey(ts => ts.ProjectId);

        modelBuilder.Entity<TestCase>()
            .HasOne(tc => tc.Project)
            .WithMany(p => p.TestCases)
            .HasForeignKey(tc => tc.ProjectId);
    }
}
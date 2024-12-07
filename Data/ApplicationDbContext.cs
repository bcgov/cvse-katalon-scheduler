using Microsoft.EntityFrameworkCore;

namespace KatalonScheduler.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public required DbSet<Project> Projects { get; set; }
    public required DbSet<TestCase> TestCases { get; set; }
}
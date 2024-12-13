public class Project
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GitRepositoryPath { get; set; } = string.Empty;
    public string GitUrl { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public DateTime LastScanned { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public List<TestSuite> TestSuites { get; set; } = new();
    public List<TestCase> TestCases { get; set; } = new();
}
public class TestCase
{
    public int Id { get; set; }
    public int TestSuiteId { get; set; }
    public int ProjectId { get; set; }  // Added for direct project reference
    public required string Name { get; set; }
    public required string FilePath { get; set; }
    
    // Navigation properties
    public TestSuite TestSuite { get; set; } = null!;
    public Project Project { get; set; } = null!;  // Added project navigation
}
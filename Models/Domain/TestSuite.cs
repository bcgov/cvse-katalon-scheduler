public class TestSuite
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }
    public int ProjectId { get; set; }
    
    // Navigation properties
    public Project Project { get; set; }
    public ICollection<TestCase> TestCases { get; set; }
    public ICollection<ScheduledTest> ScheduledTests { get; set; }
}
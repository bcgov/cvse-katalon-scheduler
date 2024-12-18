public class TestCase
{
    public int Id { get; set; }
    public int TestSuiteId { get; set; }
    public int ProjectId { get; set; }  
    public required string Name { get; set; }
    public required string FilePath { get; set; }
    
    public TestSuite TestSuite { get; set; } = null!;
    public Project Project { get; set; } = null!;  
}
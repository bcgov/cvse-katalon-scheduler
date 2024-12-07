public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public DateTime LastScanned { get; set; }
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
public class TestCase
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}
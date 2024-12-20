public class Organization
{
    public int Id { get; set; }
    public required string KatalonOrganizationId { get; set; } 
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Project> Projects { get; set; } = new();
}
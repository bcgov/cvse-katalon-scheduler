public class Organization
{
    public int Id { get; set; }
    public required string KatalonOrganizationId { get; set; }  // New field to replace ShortName
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public List<Project> Projects { get; set; } = new();
}
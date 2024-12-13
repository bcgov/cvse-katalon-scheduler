public class AdminSettings
{
    public int Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string GitRepositoryPath { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
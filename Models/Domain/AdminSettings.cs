public class AdminSettings
{
    public int Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string GitExecutablePath { get; set; } = string.Empty;
    public string KatalonPath { get; set; } = "C:\\Katalon\\Katalon_Studio_Engine_Windows_64-10.0.1\\katalonc.exe";
    public string BaseRepositoryPath { get; set; } = "C:\\KatalonProjects";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
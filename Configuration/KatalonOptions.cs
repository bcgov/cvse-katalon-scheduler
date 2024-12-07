namespace KatalonScheduler.Configuration;

public class KatalonOptions
{
    public const string Section = "Katalon";
    
    public required string RuntimeEnginePath { get; set; }
    public required string ProjectsBasePath { get; set; }
    public int ScanIntervalMinutes { get; set; } = 15;
}
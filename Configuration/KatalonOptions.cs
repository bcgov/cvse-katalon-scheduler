namespace KatalonScheduler.Configuration;

public class KatalonOptions
{
    public const string Section = "Katalon";
    
    public required string RuntimeEnginePath { get; set; }
    public string ProjectsBasePath { get; set; } = string.Empty;
    public string KatalonPath { get; set; } = string.Empty;
    public int ScanIntervalMinutes { get; set; } = 15;

    //This is set as default as it should be extracted to this location after install
    public string RuntimePath { get; set; } = "C:\\Katalon_Studio_Engine_Windows_64-9.0.0\\katalonc.exe";
    public string ProjectPath { get; set; }
    public string OrgId { get; set; }


    public string ApiKey { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;

    public string ExecutionProfile { get; set; } = string.Empty;

    public int TestOpsReleaseId { get; set; }
    public int TestOpsProjectId { get; set; }
}


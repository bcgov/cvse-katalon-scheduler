public class ScheduledTestViewModel
{
    public string JobId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string TestSuiteName { get; set; }
    public string TestSuitePath { get; set; }   
    public string TestCaseName { get; set; }   
    public string SelectedProfile { get; set; } = "Default";
    public string TestName { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LastResult { get; set; } = string.Empty;
    public string LastRunStatus { get; set; } = string.Empty;
}
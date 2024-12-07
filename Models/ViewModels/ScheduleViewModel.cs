namespace KatalonScheduler.Models.ViewModels;

public class ScheduleTestViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<TestCase> TestCases { get; set; } = new();
    public int TestCaseId { get; set; }
    
    // Schedule Configuration
    public string Hour { get; set; } = "0";
    public string Minute { get; set; } = "0";
    public string DayOfWeek { get; set; } = "*";
}
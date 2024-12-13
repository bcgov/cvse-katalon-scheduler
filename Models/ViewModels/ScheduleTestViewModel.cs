using Microsoft.AspNetCore.Mvc.Rendering;

namespace KatalonScheduler.Models.ViewModels;

public class ScheduleTestViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    
    // Test Selection
    public string SelectedTestSuite { get; set; } = string.Empty;
    public string SelectedProfile { get; set; } = "default"; 
    public List<SelectListItem> TestSuites { get; set; } = new();
    public int? TestCaseId { get; set; }
    public string SelectedTestCase { get; set; } = string.Empty;
    public List<SelectListItem> TestCases { get; set; } = new();
    
    // Schedule Configuration
    public string Hour { get; set; } = "9";
    public string Minute { get; set; } = "0";
    public string DayOfWeek { get; set; } = "*";
    
    // Additional properties from ScheduleViewModel
    public List<TestCase> AvailableTestCases { get; set; } = new();
    public string? ManualTestName { get; set; }

    public string? TempTestSuites { get; set; }
    public string? TempTestCases { get; set; }

}
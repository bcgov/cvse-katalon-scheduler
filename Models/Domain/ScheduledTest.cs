public class ScheduledTest
{
    public int Id { get; set; }
    public string JobId { get; set; }
    public int ProjectId { get; set; }
    public int TestSuiteId { get; set; }
     public string SelectedProfile { get; set; }
    public int? TestCaseId { get; set; }
    public string TestSuitePath { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public string DayOfWeek { get; set; }
    public string Schedule { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }

    // Navigation properties
    public Project Project { get; set; }
    public TestSuite TestSuite { get; set; }
    public TestCase TestCase { get; set; }
    public string LastRunStatus { get; set; }
}
public class TestExecutionLog
{
    public int Id { get; set; }
    public string JobId { get; set; }
    public int ScheduledTestId { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string Status { get; set; } // "Success", "Failed", "SchedulerError", "RuntimeError", "TestNotFound"
    public string ErrorMessage { get; set; }
    public string ExecutionDetails { get; set; } // Can store additional details like stack trace
    
    // Navigation properties
    public ScheduledTest ScheduledTest { get; set; }
}
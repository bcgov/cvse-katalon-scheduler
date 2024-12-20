public class TestExecutionLog
{
    public int Id { get; set; }
    public string JobId { get; set; }
    public int ScheduledTestId { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string Status { get; set; }
    public string ErrorMessage { get; set; }
    public string ExecutionDetails { get; set; } 
    
    public ScheduledTest ScheduledTest { get; set; }
}
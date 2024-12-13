public class DashboardViewModel
{
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public int SchedulerErrors { get; set; }
    public int RuntimeErrors { get; set; }
    public List<TestExecutionLog> RecentLogs { get; set; } = new();
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddHours(-24);
    public DateTime EndDate { get; set; } = DateTime.UtcNow;
}
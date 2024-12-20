public class Schedule
{
    public int Id { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
namespace KatalonScheduler.Models.Domain;

public class ExecutionResult
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
    public string? LogOutput { get; set; }
}
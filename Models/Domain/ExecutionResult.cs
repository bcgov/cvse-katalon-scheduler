namespace KatalonScheduler.Models.Domain;

public class ExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
    public string? LogOutput { get; set; }
    public string? ExecutionProfile { get; set; }

    public int ExitCode { get; set; }

    
}
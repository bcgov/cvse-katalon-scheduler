using KatalonScheduler.Models.Domain;

public interface IKatalonService
{
    Task<IEnumerable<Project>> ScanForProjectsAsync();
    Task<IEnumerable<TestCase>> GetTestCasesAsync(int projectId);
    Task<Project?> GetProjectAsync(int projectId);
    Task<bool> ValidateProjectAsync(string projectPath);
}

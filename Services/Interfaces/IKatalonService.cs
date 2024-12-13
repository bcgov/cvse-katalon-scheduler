using KatalonScheduler.Models.Domain;

public interface IKatalonService
{
   Task<IEnumerable<Project>> ScanForProjectsAsync();
    Task<Project?> GetProjectAsync(int projectId);
    Task<bool> ValidateProjectAsync(string projectPath);
    Task<(IEnumerable<TestSuite> Suites, IEnumerable<TestCase> Cases)> ScanProjectAsync(Project project);
}

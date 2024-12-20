namespace KatalonScheduler.Services.Interfaces;

using KatalonScheduler.Models.Domain;
public interface ITestRunnerService
{
    Task<ExecutionResult> RunTestAsync(string projectPath, string testPath,string executionProfile, int? testOpsProjectId, CancellationToken cancellationToken = default);
    Task<bool> ValidateKatalonRuntimeAsync();
}
using System.Diagnostics;
using Microsoft.Extensions.Options;
using KatalonScheduler.Configuration;
using KatalonScheduler.Services.Interfaces;
using KatalonScheduler.Models.Domain;
using KatalonScheduler.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KatalonScheduler.Services;

public class TestRunnerService : ITestRunnerService
{
    private readonly KatalonOptions _options;
    private readonly ILogger<TestRunnerService> _logger;
    private readonly ApplicationDbContext _context;

    public TestRunnerService(
        IOptions<KatalonOptions> options,
        ILogger<TestRunnerService> logger,
        ApplicationDbContext context)
    {
        _options = options.Value;
        _logger = logger;
        _context = context;
    }

    public async Task<ExecutionResult> RunTestAsync(string projectPath, string testPath, string executionProfile, int? testOpsProjectId, CancellationToken cancellationToken = default)
    {

        var paths = await GetRequiredPaths();

        var testSuite = await _context.TestSuites
            .FirstOrDefaultAsync(ts => ts.FilePath == testPath);

        if (testSuite == null)
        {
            _logger.LogError("Test suite not found for path: {Path}", testPath);
            return new ExecutionResult { Success = false, Message = "Test suite not found" };
        }

        // Then find the ScheduledTest using TestSuiteId
        var scheduledTest = await _context.ScheduledTests
            .FirstOrDefaultAsync(st => st.TestSuiteId == testSuite.Id);

        var executionLog = new TestExecutionLog
        {
            JobId = scheduledTest?.JobId ?? "manual-run",
            ScheduledTestId = scheduledTest?.Id ?? 0,
            ExecutionTime = DateTime.UtcNow,
            Status = "Pending",
            ErrorMessage = ""
        };

        if (scheduledTest != null)
        {
            // Check if there's already a running execution for this job
            var existingExecution = await _context.TestExecutionLogs
                .Where(l => l.JobId == scheduledTest.JobId &&
                            l.ExecutionTime > DateTime.UtcNow.AddMinutes(-5) &&
                            l.Status == "Success")
                .AnyAsync();

            if (existingExecution)
            {
                _logger.LogInformation("Test execution already started for job {JobId}", scheduledTest.JobId);
                return new ExecutionResult { Success = true, Message = "Test execution already in progress" };
            }
        }

        try
        {

            if (!File.Exists(paths.katalonPath))
            {
                executionLog.Status = "RuntimeError";
                executionLog.ErrorMessage = $"Katalon runtime not found at: {paths.katalonPath}";

                if (scheduledTest != null)
                {
                    scheduledTest.LastRun = DateTime.UtcNow;
                    scheduledTest.LastRunStatus = executionLog.Status;
                }

                await SaveExecutionLogAndTest(executionLog, scheduledTest);
                return new ExecutionResult { Success = false, Message = executionLog.ErrorMessage };
            }

            var projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                _logger.LogError("Invalid project path: {Path}", projectPath);
                return new ExecutionResult
                {
                    Success = false,
                    Message = "Invalid project path"
                };
            }

            var project = await _context.Projects.FindAsync(testSuite.ProjectId);
            if (project == null)
            {
                _logger.LogError("Project not found for test suite: {TestSuiteId}", testSuite.Id);
                return new ExecutionResult { Success = false, Message = "Project not found" };
            }

            // Build arguments with project-specific values
            var arguments = new[]
            {
                "-noSplash",
                "-runMode=console",
                $"-projectPath=\"{projectPath}\"",
                "-retry=0",
                $"-testSuitePath=\"{testPath.Replace('\\', '/').Replace(".ts", "")}\"",
                "-browserType=\"Chrome\"",
                $"-executionProfile=\"{(scheduledTest?.SelectedProfile ?? "default").Replace("profiles\\", "").Replace(".glbl", "")}\"",
                $"-apiKey=\"{_options.ApiKey}\"",
                $"-testOpsProjectId={project.TestOpsProjectId}",
                "--config",
                "-proxy.auth.option=NO_PROXY",
                "-proxy.system.option=NO_PROXY",
                "-proxy.system.applyToDesiredCapabilities=true",
                "-webui.autoUpdateDrivers=true"
            };

            _logger.LogInformation("Running Katalon test with arguments: {Args}", string.Join(" ", arguments));

            var startInfo = new ProcessStartInfo
            {
                FileName = paths.katalonPath,
                Arguments = string.Join(" ", arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = projectDirectory
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation(e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogError(e.Data);
                    errorBuilder.AppendLine(e.Data);
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Don't wait for exit, just check if process started successfully
                if (process.HasExited && process.ExitCode != 0)
                {
                    executionLog.Status = "Failed";
                    executionLog.ExecutionDetails = $"Process failed to start. Exit Code: {process.ExitCode}\n\nOutput:\n{outputBuilder}\n\nErrors:\n{errorBuilder}";

                    if (scheduledTest != null)
                    {
                        scheduledTest.LastRun = DateTime.UtcNow;
                        scheduledTest.LastRunStatus = executionLog.Status;
                    }

                    await SaveExecutionLogAndTest(executionLog, scheduledTest);
                    return new ExecutionResult { Success = false, Message = "Failed to start Katalon process" };
                }

                // Consider it successful if we got this far
                executionLog.Status = "Success";
                executionLog.ErrorMessage = "";
                executionLog.ExecutionDetails = $"Test execution started successfully\n\nOutput:\n{outputBuilder}";

                if (scheduledTest != null)
                {
                    scheduledTest.LastRun = DateTime.UtcNow;
                    scheduledTest.LastRunStatus = executionLog.Status;
                }

                await SaveExecutionLogAndTest(executionLog, scheduledTest);
                return new ExecutionResult { Success = true, Message = "Test execution started successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Katalon process");
                executionLog.Status = "Failed";
                executionLog.ErrorMessage = $"Process failed to start with exit code: {process.ExitCode}";
                executionLog.ExecutionDetails = $"Process failed to start. Exit Code: {process.ExitCode}\n\nOutput:\n{outputBuilder}\n\nErrors:\n{errorBuilder}";
                return new ExecutionResult { Success = false, Message = $"Error starting test: {ex.Message}" };
            }

        }
        catch (Exception ex)
        {
            executionLog.Status = "SchedulerError";
            executionLog.ErrorMessage = ex.Message;
            executionLog.ExecutionDetails = ex.StackTrace;

            if (scheduledTest != null)
            {
                scheduledTest.LastRun = DateTime.UtcNow;
                scheduledTest.LastRunStatus = executionLog.Status;
            }

            await SaveExecutionLogAndTest(executionLog, scheduledTest);
            _logger.LogError(ex, "Error running test");
            return new ExecutionResult { Success = false, Message = $"Error running test: {ex.Message}" };
        }

    }
    private async Task<(string katalonPath, string baseRepoPath)> GetRequiredPaths()
    {
        var adminSettings = await _context.AdminSettings.FirstOrDefaultAsync();

        var katalonPath = adminSettings?.KatalonPath ?? _options.KatalonPath;
        var baseRepoPath = adminSettings?.BaseRepositoryPath ?? _options.ProjectsBasePath;

        if (string.IsNullOrEmpty(katalonPath) || string.IsNullOrEmpty(baseRepoPath))
        {
            throw new InvalidOperationException("Required paths not configured in AdminSettings or appsettings.json");
        }

        return (katalonPath, baseRepoPath);
    }

    public async Task<bool> ValidateKatalonRuntimeAsync()
    {
        try
        {
            var paths = await GetRequiredPaths();
            return await Task.Run(() => File.Exists(paths.katalonPath));
        }
        catch
        {
            return false;
        }
    }

    private async Task SaveExecutionLogAndTest(TestExecutionLog log, ScheduledTest scheduledTest)
    {
        _context.TestExecutionLogs.Add(log);
        if (scheduledTest != null)
        {
            _context.ScheduledTests.Update(scheduledTest);
        }
        await _context.SaveChangesAsync();
    }
}
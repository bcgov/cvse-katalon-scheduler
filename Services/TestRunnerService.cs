using System.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using KatalonScheduler.Configuration;
using KatalonScheduler.Services.Interfaces;
using KatalonScheduler.Models.Domain;
using KatalonScheduler.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task<ExecutionResult> RunTestAsync(string projectPath, string testPath, CancellationToken cancellationToken = default)
    {
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
        JobId = scheduledTest?.JobId ?? "manual-run",  // Provide default for manual runs
        ScheduledTestId = scheduledTest?.Id ?? 0,
        ExecutionTime = DateTime.UtcNow,
        Status = "Pending"
    };

        try
        {

            if (!File.Exists(_options.KatalonPath))
            {
                executionLog.Status = "RuntimeError";
                executionLog.ErrorMessage = $"Katalon runtime not found at: {_options.KatalonPath}";

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


   
            // Build arguments with project-specific values
var arguments = new[]
{
    "-noSplash",
    "-runMode=console",
    $"-projectPath=\"{projectPath}\"",
    "-retry=0",
    $"-testSuitePath=\"{testPath}\"",  // Remove "Test Suites/" prefix since it's already in the path
    "-browserType=\"Chrome\"",
    $"-executionProfile=\"{_options.ExecutionProfile}\"",
    $"-apiKey=\"{_options.ApiKey}\"",
    $"-testOpsReleaseId={_options.TestOpsReleaseId}",
    $"-testOpsProjectId={_options.TestOpsProjectId}",
    "--config",
    "-proxy.auth.option=NO_PROXY",
    "-proxy.system.option=NO_PROXY",
    "-proxy.system.applyToDesiredCapabilities=true",
    "-webui.autoUpdateDrivers=true"
};

            _logger.LogInformation("Running Katalon test with arguments: {Args}", string.Join(" ", arguments));

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.KatalonPath,
                Arguments = string.Join(" ", arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = projectDirectory
            };

            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (sender, e) => _logger.LogInformation(e.Data);
            process.ErrorDataReceived += (sender, e) => _logger.LogError(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            executionLog.Status = process.ExitCode == 0 ? "Success" : "Failed";
            executionLog.ExecutionDetails = $"Exit Code: {process.ExitCode}";

            if (scheduledTest != null)
            {
                scheduledTest.LastRun = DateTime.UtcNow;
                scheduledTest.LastRunStatus = executionLog.Status;
            }

            await SaveExecutionLogAndTest(executionLog, scheduledTest);


            return new ExecutionResult
            {
                Success = process.ExitCode == 0,
                Message = process.ExitCode == 0 ? "Test completed successfully" : "Test failed"
            };
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

    public async Task<bool> ValidateKatalonRuntimeAsync()
    {
        return await Task.Run(() => File.Exists(_options.KatalonPath));
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
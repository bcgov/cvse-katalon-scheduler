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
        // Validate Katalon runtime exists
        if (!File.Exists(_options.KatalonPath))
        {
            _logger.LogError("Katalon runtime not found at: {Path}", _options.KatalonPath);
            return new ExecutionResult
            {
                Success = false,
                Message = $"Katalon runtime not found at: {_options.KatalonPath}"
            };
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

        // Find the scheduled test record
        var scheduledTest = await _context.ScheduledTests
            .FirstOrDefaultAsync(st => st.TestSuitePath == testPath);
        try
        {
            // Build arguments with project-specific values
            var arguments = new[]
            {
        "-noSplash",
        "-runMode=console",
        $"-projectPath=\"{projectPath}\"",
        "-retry=0",
        $"-testSuitePath=\"{testPath}\"",
        "-browserType=\"Chrome\"",
        $"-executionProfile=\"{_options.ExecutionProfile}\"",
        $"-apiKey=\"{_options.ApiKey}\"",
        $"-testOpsProjectId={projectPath.Split('\\').Last().Split('_').First()}",
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

            // Update the scheduled test status
            if (scheduledTest != null)
            {
                scheduledTest.LastRun = DateTime.UtcNow;
                scheduledTest.LastRunStatus = process.ExitCode == 0 ? "Success" : "Failed";
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new ExecutionResult
            {
                Success = process.ExitCode == 0,
                Message = process.ExitCode == 0 ? "Test completed successfully" : "Test failed"
            };
        }
        catch (Exception ex)
        {
            // Update status on error
            if (scheduledTest != null)
            {
                scheduledTest.LastRun = DateTime.UtcNow;
                scheduledTest.LastRunStatus = "Failed";
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogError(ex, "Error running test");
            return new ExecutionResult
            {
                Success = false,
                Message = $"Error running test: {ex.Message}"
            };
        }
    }

    public async Task<bool> ValidateKatalonRuntimeAsync()
    {
        return await Task.Run(() => File.Exists(_options.KatalonPath));
    }
}
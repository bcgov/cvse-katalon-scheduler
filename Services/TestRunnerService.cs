using System.Diagnostics;
using KatalonScheduler.Configuration;
using KatalonScheduler.Models.Domain;
using KatalonScheduler.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KatalonScheduler.Services;

public class TestRunnerService : ITestRunnerService
{
    private readonly KatalonOptions _options;
    private readonly ILogger<TestRunnerService> _logger;

    public TestRunnerService(IOptions<KatalonOptions> options, ILogger<TestRunnerService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunTestAsync(string projectPath, string testPath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.RuntimeEnginePath,
            Arguments = $"-projectPath \"{projectPath}\" -testSuitePath \"{testPath}\" -noSplash -runMode=console",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
            var output = new List<string>();
            var errors = new List<string>();

            process.OutputDataReceived += (sender, e) => 
            {
                if (e.Data != null) output.Add(e.Data);
            };
            process.ErrorDataReceived += (sender, e) => 
            {
                if (e.Data != null) errors.Add(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return new ExecutionResult
            {
                Success = process.ExitCode == 0,
                Message = process.ExitCode == 0 ? "Test executed successfully" : "Test execution failed",
                LogOutput = string.Join(Environment.NewLine, output.Concat(errors))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Katalon test");
            return new ExecutionResult
            {
                Success = false,
                Message = $"Error running test: {ex.Message}"
            };
        }
    }

    public async Task<bool> ValidateKatalonRuntimeAsync()
    {
        if (!File.Exists(_options.RuntimeEnginePath))
            return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _options.RuntimeEnginePath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            await process!.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Katalon Runtime");
            return false;
        }
    }
}
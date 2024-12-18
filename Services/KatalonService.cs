using KatalonScheduler.Configuration;
using KatalonScheduler.Data;
using KatalonScheduler.Models.Domain;
using KatalonScheduler.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace KatalonScheduler.Services;

public class KatalonService : IKatalonService
{

    private readonly ApplicationDbContext _context;
    private readonly KatalonOptions _options;
    private readonly ILogger<KatalonService> _logger;
    private readonly GitOptions _gitOptions;

    public KatalonService(
        ApplicationDbContext context,
        IOptions<KatalonOptions> options,
        IOptions<GitOptions> gitOptions,
        ILogger<KatalonService> logger)
    {
        _context = context;
        _options = options.Value;
        _gitOptions = gitOptions.Value;
        _logger = logger;

        _logger.LogInformation("KatalonService initialized with GitOptions: {@GitOptions}", _gitOptions);
    }


    public async Task<IEnumerable<Project>> ScanForProjectsAsync()
    {
        try
        {
            return await _context.Projects
                .Include(p => p.TestCases)
                .Include(p => p.TestSuites)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for projects");
            return Enumerable.Empty<Project>();
        }
    }

    public async Task<(IEnumerable<TestSuite> Suites, IEnumerable<TestCase> Cases)> ScanProjectAsync(Project project)
    {
        try
        {
            if (string.IsNullOrEmpty(project.GitUrl))
            {
                _logger.LogError("GitUrl is missing for project {ProjectId}", project.Id);
                throw new InvalidOperationException("Git URL is required");
            }

            if (string.IsNullOrEmpty(_gitOptions.AccessToken))
            {
                _logger.LogError("Git access token is not configured");
                throw new InvalidOperationException("Git access token is not configured");
            }

            // Sanitize the repository path before git operations
            project.GitRepositoryPath = Path.Combine(_gitOptions.BaseRepositoryPath,
                string.Join("_", project.Name.Trim().Split(Path.GetInvalidFileNameChars())));

            await UpdateGitRepository(project);
            var basePath = project.GitRepositoryPath;
            var testSuitesPath = Path.Combine(project.GitRepositoryPath, "Test Suites");

            if (!Directory.Exists(testSuitesPath))
            {
                _logger.LogWarning("Test suites directory not found: {Path}", testSuitesPath);
                return (Array.Empty<TestSuite>(), Array.Empty<TestCase>());
            }

            // Get all .ts files recursively
            var suites = Directory.GetFiles(testSuitesPath, "*.ts", SearchOption.AllDirectories)
                .Select(suiteFile =>
                {
                    var relativePath = suiteFile.Replace(basePath, "").TrimStart('\\', '/');
                    return new TestSuite
                    {
                        ProjectId = project.Id,
                        Name = $"{Path.GetDirectoryName(relativePath)?.Replace("Test Suites\\", "")}\\{Path.GetFileNameWithoutExtension(suiteFile)}",
                        FilePath = relativePath
                    };
                })
                .ToList();

            _logger.LogInformation("Found {Count} test suites in {Path}", suites.Count, testSuitesPath);

            // Update existing test suites in DB
            var existingSuites = await _context.TestSuites
                .Where(ts => ts.ProjectId == project.Id)
                .ToListAsync();

            _context.TestSuites.RemoveRange(existingSuites);
            _context.TestSuites.AddRange(suites);

            project.LastScanned = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (suites, Array.Empty<TestCase>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning project {ProjectId}", project.Id);
            throw;
        }
    }

    private async Task UpdateGitRepository(Project project)
    {
        var gitUrl = project.GitUrl.Replace("https://", $"https://oauth2:{_gitOptions.AccessToken}@");

        if (!Directory.Exists(project.GitRepositoryPath))
        {
            var cloneProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = $"clone \"{gitUrl}\" \"{project.GitRepositoryPath}\"",  
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            await RunGitCommand(cloneProcess);
        }
        else
        {
            var pullProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = "pull",  
                    WorkingDirectory = project.GitRepositoryPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            await RunGitCommand(pullProcess);
        }
    }

    private async Task RunGitCommand(Process process)
    {
        try
        {
            // Use full path to git executable
            process.StartInfo.FileName = @"C:\Program Files\Git\bin\git.exe";

            _logger.LogInformation("Running Git command: {Command} {Args}",
                process.StartInfo.FileName,
                process.StartInfo.Arguments);

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Git command failed with exit code {Code}. Error: {Error}",
                    process.ExitCode, error);
                throw new InvalidOperationException($"Git command failed: {error}");
            }

            _logger.LogInformation("Git command completed successfully. Output: {Output}", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git command failed to execute");
            throw;
        }
    }

    //behind feature flag not currently turned on until we want it
    private async Task ScanTestCasesForProject(Project project)
    {
        var testCasesPath = Path.Combine(Path.GetDirectoryName(project.GitRepositoryPath)!, "Test Cases");
        if (!Directory.Exists(testCasesPath))
        {
            _logger.LogWarning("Test Cases directory not found for project: {Path}", testCasesPath);
            return;
        }

        var testFiles = Directory.GetFiles(testCasesPath, "*.tc", SearchOption.AllDirectories);
        foreach (var testFile in testFiles)
        {
            var relativePath = testFile.Replace(testCasesPath + Path.DirectorySeparatorChar, "");
            var testName = Path.GetFileNameWithoutExtension(relativePath);

            if (!project.TestCases.Any(t => t.FilePath == testFile))
            {
                project.TestCases.Add(new TestCase
                {
                    Name = testName,
                    FilePath = testFile,
                    ProjectId = project.Id
                });
            }
        }

        await _context.SaveChangesAsync();
    }
    public async Task<IEnumerable<TestCase>> GetTestCasesAsync(int projectId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.TestCases)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                _logger.LogWarning("Project not found: {ProjectId}", projectId);
                return Enumerable.Empty<TestCase>();
            }

            // Check if project path still exists
            if (!await ValidateProjectAsync(project.GitRepositoryPath))
            {
                _logger.LogWarning("Project path no longer valid: {Path}", project.GitRepositoryPath);
                return Enumerable.Empty<TestCase>();
            }

            // Rescan test cases if needed
            var testCasesPath = Path.Combine(Path.GetDirectoryName(project.GitRepositoryPath)!, "Test Cases");
            if (Directory.Exists(testCasesPath))
            {
                var testFiles = Directory.GetFiles(testCasesPath, "*.tc", SearchOption.AllDirectories);
                foreach (var testFile in testFiles)
                {
                    var testName = Path.GetFileNameWithoutExtension(testFile);
                    if (!project.TestCases.Any(t => t.FilePath == testFile))
                    {
                        project.TestCases.Add(new TestCase
                        {
                            Name = testName,
                            FilePath = testFile,
                            ProjectId = project.Id
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return project.TestCases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test cases for project {ProjectId}", projectId);
            return Enumerable.Empty<TestCase>();
        }
    }

    public async Task<bool> ValidateProjectAsync(string projectPath)
    {
        if (!await Task.Run(() => File.Exists(projectPath)))
            return false;

        try
        {
            // Add more validation logic here
            return Path.GetExtension(projectPath).Equals(".prj", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating project at {Path}", projectPath);
            return false;
        }
    }

    public async Task<Project?> GetProjectAsync(int projectId)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<ExecutionResult> RunTestAsync(int testCaseId)
    {
        var testCase = await _context.TestCases
            .Include(tc => tc.Project)
            .FirstOrDefaultAsync(tc => tc.Id == testCaseId);

        if (testCase == null)
            return new ExecutionResult { Success = false, Message = "Test case not found" };

        // Delegate to TestRunnerService in the next implementation
        throw new NotImplementedException();
    }
}
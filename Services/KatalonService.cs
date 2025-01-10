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

        _logger.LogInformation("KatalonService initialized with GitOptions: {@GitOptions}", SanitizeLogParam(_gitOptions.ToString()));
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
                _logger.LogError("GitUrl is missing for project {ProjectId}", SanitizeLogParam(project.Id.ToString()));
                throw new InvalidOperationException("Git URL is required");
            }

            if (string.IsNullOrEmpty(_gitOptions.AccessToken))
            {
                _logger.LogError("Git access token is not configured");
                throw new InvalidOperationException("Git access token is not configured");
            }

            // // Sanitize the repository path before git operations
            // project.GitRepositoryPath = Path.Combine(_gitOptions.BaseRepositoryPath,
            //     string.Join("_", project.Name.Trim().Split(Path.GetInvalidFileNameChars())));

            project.GitRepositoryPath = Path.GetFullPath(project.GitRepositoryPath);
            await UpdateGitRepository(project);
            var basePath = project.GitRepositoryPath;
            var testSuitesPath = Path.Combine(project.GitRepositoryPath, "Test Suites");

            if (!Directory.Exists(testSuitesPath))
            {
                _logger.LogWarning("Test suites directory not found: {Path}", SanitizeLogParam(testSuitesPath));
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

            _logger.LogInformation("Found {Count} test suites in {Path}", suites.Count, SanitizeLogParam(testSuitesPath));

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
            _logger.LogError(ex, "Error scanning project {ProjectId}", SanitizeLogParam(project.Id.ToString()));
            throw;
        }
    }

    private async Task UpdateGitRepository(Project project)
    {
        try
        {
            var gitUrl = project.GitUrl.Replace("https://", $"https://oauth2:{_gitOptions.AccessToken}@");
            
            if (!Uri.TryCreate(gitUrl, UriKind.Absolute, out var validatedGitUrl) || (validatedGitUrl.Scheme != Uri.UriSchemeHttp && validatedGitUrl.Scheme != Uri.UriSchemeHttps))
            {
                _logger.LogError("Invalid Git URL: {GitUrl}", SanitizeLogParam(project.GitUrl));
                throw new InvalidOperationException("Invalid Git URL");
            }
            _logger.LogInformation("Starting git operation for project: {Name} at path: {Path}",
                SanitizeLogParam(project.Name), SanitizeLogParam(project.GitRepositoryPath));

            if (!Directory.Exists(project.GitRepositoryPath))
            {
                _logger.LogInformation("Repository directory not found, cloning from: {GitUrl} to {Path}",
                    SanitizeLogParam(project.GitUrl), SanitizeLogParam(project.GitRepositoryPath));

                var cloneProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"clone {gitUrl} \"{project.GitRepositoryPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                await RunGitCommand(cloneProcess);
            }
            else
            {
                _logger.LogInformation("Repository exists, pulling latest changes");
                var pullProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "pull",
                        WorkingDirectory = Path.GetFullPath(project.GitRepositoryPath),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                await RunGitCommand(pullProcess);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git operation failed for project: {Name}", SanitizeLogParam(project.Name));
            throw;
        }
    }

    private async Task RunGitCommand(Process process)
    {
        try
        {
            var adminSettings = await _context.AdminSettings.FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(adminSettings?.GitExecutablePath))
            {
                throw new InvalidOperationException("Git executable path not configured in AdminSettings");
            }
            // Use full path to git executable
            process.StartInfo.FileName = adminSettings.GitExecutablePath;

            _logger.LogInformation("Running Git command: {Command} {Args}",
                SanitizeLogParam(process.StartInfo.FileName),
                SanitizeLogParam(process.StartInfo.Arguments));

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Git command failed with exit code {Code}. Error: {Error}",
                    process.ExitCode, SanitizeLogParam(error));
                throw new InvalidOperationException($"Git command failed: {error}");
            }

            _logger.LogInformation("Git command completed successfully. Output: {Output}", SanitizeLogParam(output));
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
            _logger.LogWarning("Test Cases directory not found for project: {Path}", SanitizeLogParam(testCasesPath));
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
                _logger.LogWarning("Project not found: {ProjectId}", SanitizeLogParam(projectId.ToString()));
                return Enumerable.Empty<TestCase>();
            }

            // Check if project path still exists
            if (!await ValidateProjectAsync(project.GitRepositoryPath))
            {
                _logger.LogWarning("Project path no longer valid: {Path}", SanitizeLogParam(project.GitRepositoryPath));
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
            _logger.LogError(ex, "Error getting test cases for project {ProjectId}", SanitizeLogParam(projectId.ToString()));
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
            _logger.LogError(ex, "Error validating project at {Path}", SanitizeLogParam(projectPath));
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

    private string SanitizeLogParam(string message)
    {
        return message.Replace("\n", " ").Replace("\r", " ");
    }
}
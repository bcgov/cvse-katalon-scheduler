using KatalonScheduler.Configuration;
using KatalonScheduler.Data;
using KatalonScheduler.Models.Domain;
using KatalonScheduler.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace KatalonScheduler.Services;

public class KatalonService : IKatalonService
{
    private readonly ApplicationDbContext _context;
    private readonly KatalonOptions _options;
    private readonly ILogger<KatalonService> _logger;

    public KatalonService(
        ApplicationDbContext context,
        IOptions<KatalonOptions> options,
        ILogger<KatalonService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<Project>> ScanForProjectsAsync()
    {
        
    if (!Directory.Exists(_options.ProjectsBasePath))
    {
        _logger.LogWarning("Projects directory does not exist. Creating: {Path}", _options.ProjectsBasePath);
        Directory.CreateDirectory(_options.ProjectsBasePath);
        return Enumerable.Empty<Project>();
    }

    var projectFiles = Directory.GetFiles(_options.ProjectsBasePath, "*.prj", SearchOption.AllDirectories);
    var projects = new List<Project>();

        foreach (var projectFile in projectFiles)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            var existingProject = await _context.Projects
                .FirstOrDefaultAsync(p => p.Path == projectFile);

            if (existingProject == null)
            {
                var project = new Project
                {
                    Name = projectName,
                    Path = projectFile,
                    LastScanned = DateTime.UtcNow
                };
                _context.Projects.Add(project);
                projects.Add(project);
            }
            else
            {
                existingProject.LastScanned = DateTime.UtcNow;
                projects.Add(existingProject);
            }
        }

        await _context.SaveChangesAsync();
        return projects;
    }

    public async Task<IEnumerable<TestCase>> GetTestCasesAsync(int projectId)
    {
        return await _context.TestCases
            .Where(tc => tc.ProjectId == projectId)
            .ToListAsync();
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
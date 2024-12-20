using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cronos;
using KatalonScheduler.Services.Interfaces;
using KatalonScheduler.Data;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc.Rendering;
using KatalonScheduler.Models.ViewModels;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace KatalonScheduler.Controllers;

public class ScheduleController : Controller
{
    private readonly IKatalonService _katalonService;
    private readonly ITestRunnerService _testRunnerService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScheduleController> _logger;
    private readonly IOptions<GitOptions> _gitOptions;

    public ScheduleController(
        IKatalonService katalonService,
        ITestRunnerService testRunnerService,
        ApplicationDbContext context,
        ILogger<ScheduleController> logger,
        IOptions<GitOptions> gitOptions)
    {
        _katalonService = katalonService;
        _testRunnerService = testRunnerService;
        _context = context;
        _logger = logger;
        _gitOptions = gitOptions;
    }

    public async Task<IActionResult> Projects()
    {
        var organizations = await _context.Organizations
            .Include(o => o.Projects)
                .ThenInclude(p => p.TestSuites)
            .Include(o => o.Projects)
                .ThenInclude(p => p.TestCases)
            .ToListAsync();
        return View(organizations);
    }

    public async Task<IActionResult> Create(int? projectId = null)
    {
        if (!projectId.HasValue)
            return RedirectToAction(nameof(Projects));

        var project = await _katalonService.GetProjectAsync(projectId.Value);
        if (project == null)
            return NotFound();

        // Get test suites from database
        var dbTestSuites = await _context.TestSuites
            .Where(ts => ts.ProjectId == projectId)
            .Select(ts => new SelectListItem
            {
                Value = ts.FilePath,
                Text = ts.Name
            })
            .ToListAsync();

        // Get test suites from filesystem
        var testSuitesPath = Path.Combine(project.GitRepositoryPath, "Test Suites");
        var fsTestSuites = Directory.Exists(testSuitesPath)
            ? Directory.GetFiles(testSuitesPath, "*.ts", SearchOption.AllDirectories)
                .Select(path => new SelectListItem
                {
                    Value = path,
                    Text = Path.GetFileNameWithoutExtension(path)
                })
                .ToList()
            : new List<SelectListItem>();

        // Combine both lists avoiding duplicates
        var testSuites = dbTestSuites
            .Union(fsTestSuites, new SelectListItemComparer())
            .ToList();

        var viewModel = new ScheduleTestViewModel
        {
            ProjectId = projectId.Value,
            ProjectName = project.Name,
            TestSuites = testSuites
        };

        return View(viewModel);
    }

    private class SelectListItemComparer : IEqualityComparer<SelectListItem>
    {
        public bool Equals(SelectListItem x, SelectListItem y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode(SelectListItem obj)
        {
            return obj.Value.GetHashCode();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _context.Projects
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        return Json(projects);
    }

    [HttpPost]
    public async Task<IActionResult> AddTestSuite([FromBody] AddTestSuiteViewModel model)
    {
        var project = await _context.Projects.FindAsync(model.ProjectId);
        if (project == null)
            return NotFound();

        var testSuite = new TestSuite
        {
            ProjectId = model.ProjectId,
            Name = model.Name,
            FilePath = model.FilePath
        };

        _context.TestSuites.Add(testSuite);
        await _context.SaveChangesAsync();

        return Ok(new { id = testSuite.Id, name = testSuite.Name });
    }

    [HttpPost]
    public async Task<IActionResult> AddTestCase([FromBody] AddTestCaseViewModel model)
    {
        var testSuite = await _context.TestSuites
            .Include(ts => ts.Project)
            .FirstOrDefaultAsync(ts => ts.Id == model.TestSuiteId);

        if (testSuite == null)
            return NotFound();

        var testCase = new TestCase
        {
            TestSuiteId = model.TestSuiteId,
            ProjectId = testSuite.ProjectId,
            Name = model.Name,
            FilePath = model.FilePath
        };

        _context.TestCases.Add(testCase);
        await _context.SaveChangesAsync();

        return Ok(new { id = testCase.Id, name = testCase.Name });
    }

    [HttpGet("Schedule/GetTestSuites/{projectId}")]
    public async Task<IActionResult> GetTestSuites(int projectId)
    {
        var testSuites = await _context.TestSuites
            .Where(ts => ts.ProjectId == projectId)
            .Select(ts => new
            {
                id = ts.Id,
                name = ts.Name.Replace("Test Suites\\", ""),
                filePath = ts.FilePath,
                projectId = ts.ProjectId
            })
            .ToListAsync();

        return Json(testSuites);
    }



    [HttpGet]
    public async Task<IActionResult> GetScheduledTests()
    {
        var groupedTests = await _context.ScheduledTests
            .Include(st => st.Project)
            .Include(st => st.TestSuite)
            .GroupBy(st => st.TestSuiteId)
            .Select(group => new TestSuiteGroupViewModel
            {
                TestSuiteName = group.First().TestSuite.Name.Replace("Test Suites\\", ""),
                TestSuitePath = group.First().TestSuite.FilePath,
                TestSuite = group.First().TestSuite,
                ScheduledRuns = group.Select(st => new ScheduledTestViewModel
                {
                    JobId = st.JobId,
                    ProjectName = st.Project.Name,
                    TestSuiteName = st.TestSuite.Name.Replace("Test Suites\\", ""),
                    TestSuitePath = st.TestSuite.FilePath,
                    SelectedProfile = st.SelectedProfile ?? "Default",
                    Schedule = $"{st.Hour:D2}:{st.Minute:D2} on {st.DayOfWeek}",
                    IsActive = st.IsActive,
                    LastRun = st.LastRun,
                    LastRunStatus = st.LastRunStatus ?? "N/A",
                    NextRun = GetNextRunTime($"{st.Minute} {st.Hour} * * {st.DayOfWeek}")
                }).ToList()
            })
            .ToListAsync();

        return Json(groupedTests);
    }

    private static DateTime? GetNextRunTime(string cronExpression)
    {
        try
        {
            var expression = CronExpression.Parse(cronExpression);
            return expression.GetNextOccurrence(DateTime.Now);
        }
        catch
        {
            return null;
        }
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromForm] ScheduleTestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state: {errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        try
        {
            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
                return NotFound();

            // First try to find by ID (for existing test suites)
            var testSuite = await _context.TestSuites
                .FirstOrDefaultAsync(ts => ts.Id.ToString() == model.SelectedTestSuite);

            // If not found by ID try to find by file path
            if (testSuite == null)
            {
                testSuite = await _context.TestSuites
                    .FirstOrDefaultAsync(ts => ts.FilePath == model.SelectedTestSuite);
            }

            if (testSuite == null && !string.IsNullOrEmpty(model.TempTestSuites))
            {
                testSuite = new TestSuite
                {
                    ProjectId = model.ProjectId,
                    Name = Path.GetFileNameWithoutExtension(model.SelectedTestSuite),
                    FilePath = model.SelectedTestSuite
                };
                _context.TestSuites.Add(testSuite);
                await _context.SaveChangesAsync();
            }

            if (testSuite == null)
            {
                _logger.LogWarning("Test suite not found. SelectedTestSuite: {suite}, ProjectId: {projectId}",
                    model.SelectedTestSuite, model.ProjectId);
                return BadRequest("Test suite not found");
            }

            var jobId = $"test-{Guid.NewGuid()}";

            // Map day of week to number if needed
            var dayOfWeek = model.DayOfWeek switch
            {
                "Mon" => "1",
                "Tue" => "2",
                "Wed" => "3",
                "Thu" => "4",
                "Fri" => "5",
                "Sat" => "6",
                "Sun" => "0",
                _ => model.DayOfWeek
            };

            // Create the scheduled test record
            var scheduledTest = new ScheduledTest
            {
                JobId = jobId,
                ProjectId = model.ProjectId,
                TestSuiteId = testSuite.Id,
                SelectedProfile = model.SelectedProfile ?? "default",
                TestSuitePath = testSuite.FilePath,
                Hour = int.Parse(model.Hour),
                Minute = int.Parse(model.Minute),
                DayOfWeek = dayOfWeek,
                Schedule = $"{model.Hour:D2}:{model.Minute:D2} on {dayOfWeek}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastRunStatus = "Pending"
            };

            _context.ScheduledTests.Add(scheduledTest);
            await _context.SaveChangesAsync();

            // Create Hangfire job
            var cron = $"{model.Minute} {model.Hour} * * {dayOfWeek}";
            RecurringJob.AddOrUpdate(
                jobId,
                () => _testRunnerService.RunTestAsync(
                    project.ProjectPath,
                    testSuite.FilePath,
                    scheduledTest.SelectedProfile,
                    (int?)project.TestOpsProjectId,
                    CancellationToken.None),
                cron);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling test");
            return BadRequest("Error scheduling test");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProject(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound();

        return Json(new
        {
            id = project.Id,
            name = project.Name,
            gitUrl = project.GitUrl,
            projectPath = project.ProjectPath,
            gitRepositoryPath = project.GitRepositoryPath,
            testOpsProjectId = project.TestOpsProjectId,
            lastScanned = project.LastScanned,
            organizationId = project.OrganizationId
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProject(int id, ProjectViewModel model)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound();

        project.Name = model.Name;
        project.GitUrl = model.GitUrl;
        project.GitRepositoryPath = model.GitRepositoryPath;
        project.TestOpsProjectId = model.TestOpsProjectId;

        // Only update ProjectPath if it's provided
        if (!string.IsNullOrEmpty(model.ProjectPath))
        {
            project.ProjectPath = model.ProjectPath;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectPrjPath(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound();

        try
        {
            var adminSettings = await _context.AdminSettings.FirstOrDefaultAsync();
            // Check if directory exists first
            if (!Directory.Exists( project.GitRepositoryPath))
            {
                return NotFound(new { error = $"Project directory not found: {project.GitRepositoryPath}" });
            }

            // Search for .prj file in the Git repository directory
            var prjFiles = Directory.GetFiles( project.GitRepositoryPath, "*.prj", SearchOption.AllDirectories);

            if (!prjFiles.Any())
                return NotFound(new { error = "No .prj file found in repository" });

            // Use the first .prj file found
            var projectPath = prjFiles.First();

            return Json(new { projectPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for .prj file in {Path}", project.GitRepositoryPath);
            return BadRequest(new { error = "Error accessing project directory" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProjectPath(int id, [FromBody] UpdateProjectPathRequest request)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound();

        project.ProjectPath = request.ProjectPath;
        await _context.SaveChangesAsync();

        return Ok();
    }

    public class UpdateProjectPathRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
    }



    [HttpGet]
    public async Task<IActionResult> GetTestCases(int suiteId)
    {
        var testCases = await _context.TestCases
            .Where(tc => tc.TestSuiteId == suiteId)
            .Select(tc => new { id = tc.Id, name = tc.Name })
            .ToListAsync();

        return Json(testCases);
    }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var groupedTests = await _context.ScheduledTests
            .Include(st => st.Project)
            .Include(st => st.TestSuite) 
            .GroupBy(st => st.TestSuiteId)
            .Select(group => new TestSuiteGroupViewModel
            {
                TestSuiteName = group.First().TestSuite.Name.Replace("Test Suites\\", ""),
                TestSuitePath = group.First().TestSuite.FilePath,
                TestSuite = group.First().TestSuite,
                ScheduledRuns = group.Select(st => new ScheduledTestViewModel
                {
                    JobId = st.JobId,
                    ProjectName = st.Project.Name,
                    TestSuiteName = st.TestSuite.Name.Replace("Test Suites\\", ""),
                    TestSuitePath = st.TestSuite.FilePath,
                    SelectedProfile = st.SelectedProfile ?? "Default",
                    Schedule = $"{st.Hour:D2}:{st.Minute:D2} on {st.DayOfWeek}",
                    IsActive = st.IsActive,
                    LastRun = st.LastRun,
                    NextRun = GetNextRunTime($"{st.Minute} {st.Hour} * * {st.DayOfWeek}")
                }).ToList()
            })
            .ToListAsync();



        return View(groupedTests);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(string jobId)
    {
        var manager = new RecurringJobManager();
        var job = JobStorage.Current.GetConnection().GetRecurringJobs()
            .FirstOrDefault(j => j.Id == jobId);

        // Find the scheduled test in database
        var scheduledTest = await _context.ScheduledTests
            .Include(st => st.Project)
            .Include(st => st.TestSuite)
            .FirstOrDefaultAsync(st => st.JobId == jobId);

        if (scheduledTest == null)
            return NotFound();

        if (job?.NextExecution.HasValue ?? false)
        {
            // Job is active pause it
            manager.RemoveIfExists(jobId);
            scheduledTest.IsActive = false;
        }
        else
        {
            // Job is paused resume it
            var cron = $"{scheduledTest.Minute} {scheduledTest.Hour} * * {scheduledTest.DayOfWeek}";
            RecurringJob.AddOrUpdate(
                jobId,
                () => _testRunnerService.RunTestAsync(
                    scheduledTest.Project.ProjectPath,
                    scheduledTest.TestSuite.FilePath,
                    scheduledTest.SelectedProfile,
                    (int?)scheduledTest.Project.TestOpsProjectId,
                    CancellationToken.None),
                cron);
            scheduledTest.IsActive = true;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    [Route("[controller]/Delete/{jobId}")]
    public async Task<IActionResult> Delete(string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
        {
            _logger.LogWarning("Attempted to delete schedule with null jobId");
            return BadRequest("JobId cannot be null or empty");
        }

        try
        {
            // Find the scheduled test first
            var scheduledTest = await _context.ScheduledTests
                .FirstOrDefaultAsync(st => st.JobId == jobId);

            if (scheduledTest == null)
            {
                _logger.LogWarning("Scheduled test not found for jobId: {JobId}", jobId);
                return NotFound();
            }

            // Remove from Hangfire using manager
            var manager = new RecurringJobManager();
            var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();

            if (recurringJobs.Any(j => j.Id == jobId))
            {
                manager.RemoveIfExists(jobId);
            }

            // Remove from database
            _context.ScheduledTests.Remove(scheduledTest);
            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule {JobId}", jobId);
            return StatusCode(500, "Error deleting schedule");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string jobId)
    {
        var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();
        var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);


        if (job == null)
            return NotFound();

        var testId = job.Id.Replace("test-", "");
        if (!int.TryParse(testId, out var id))
            return NotFound();

        var testCase = await _context.TestCases
            .Include(tc => tc.Project)
            .FirstOrDefaultAsync(tc => tc.Id == id);

        if (testCase == null)
            return NotFound();

        var schedule = job.Cron.Split(' ');
        var viewModel = new ScheduleTestViewModel
        {
            TestCaseId = testCase.Id,
            ProjectId = testCase.Project.Id,
            ProjectName = testCase.Project.Name,
            Minute = schedule[0],
            Hour = schedule[1],
            DayOfWeek = schedule[4]
        };

        return View("Create", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetTestSuite(int id)
    {
        var testSuite = await _context.TestSuites
            .Include(ts => ts.TestCases)
            .Include(ts => ts.ScheduledTests)
            .Include(ts => ts.Project)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (testSuite == null)
            return NotFound();

        var scheduledTests = testSuite.ScheduledTests.ToList();
        var firstSchedule = scheduledTests.FirstOrDefault();

        return Json(new
        {
            id = testSuite.Id,
            name = testSuite.Name,
            projectId = testSuite.ProjectId,
            projectName = testSuite.Project.Name,
            filePath = testSuite.FilePath,
            hour = firstSchedule?.Hour ?? 0,
            minute = firstSchedule?.Minute ?? 0,
            days = scheduledTests.Select(st => st.DayOfWeek).ToArray(),
            isActive = firstSchedule?.IsActive ?? true
        });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTestSuite(int id)
    {
        var testSuite = await _context.TestSuites
            .Include(ts => ts.ScheduledTests)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (testSuite == null)
            return NotFound();

        // Delete all scheduled tests for this suite
        foreach (var scheduledTest in testSuite.ScheduledTests)
        {
            RecurringJob.RemoveIfExists(scheduledTest.JobId);
        }

        _context.TestSuites.Remove(testSuite);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> AddProject(ProjectViewModel projectVM)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid project data: {Errors}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            TempData["Error"] = "Invalid project data";
            return RedirectToAction(nameof(Projects));
        }

        var adminSettings = await _context.AdminSettings.FirstOrDefaultAsync();
        var baseRepoPath = adminSettings?.BaseRepositoryPath;

        var project = new Project
        {
            Name = SanitizeProjectName(projectVM.Name),
            GitUrl = projectVM.GitUrl,
            GitRepositoryPath = Path.Combine(baseRepoPath, SanitizeProjectName(projectVM.Name)),
            OrganizationId = projectVM.OrganizationId,
            TestOpsProjectId = projectVM.TestOpsProjectId,
            LastScanned = DateTime.UtcNow
        };

        try
        {
            var organization = await _context.Organizations.FindAsync(project.OrganizationId);
            if (organization == null)
            {
                _logger.LogWarning("Organization not found: {OrganizationId}", project.OrganizationId);
                TempData["Error"] = "Organization not found";
                return RedirectToAction(nameof(Projects));
            }
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Try to sync the project following the same steps as manual scan
            try
            {
                // Sync/clone repository
                await _katalonService.ScanProjectAsync(project);
                //  Get .prj file path
                var prjFiles = Directory.GetFiles(project.GitRepositoryPath, "*.prj", SearchOption.AllDirectories);
                if (prjFiles.Any())
                {
                    //  Update project path
                    project.ProjectPath = prjFiles.First();
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Project added and synced successfully";
                }
                else
                {
                    TempData["Warning"] = "Project added but no .prj file found. Please check the repository.";
                }
            }
            catch (Exception syncEx)
            {
                _logger.LogError(syncEx, "Error syncing new project: {@Project}", project);
                TempData["Warning"] = "Project added but sync failed. Please check your Git URL and try syncing manually.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding project: {@Project}", project);
            TempData["Error"] = "Error adding project";
        }

        return RedirectToAction(nameof(Projects));
    }

    [HttpPost]
    public async Task<IActionResult> ScanProject(int id)
    {
        _logger.LogInformation("Starting project scan for ID: {ProjectId}", id);
        try
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                _logger.LogWarning("Project not found with ID: {ProjectId}", id);
                return NotFound();
            }

            _logger.LogInformation("Found project: {@Project}", project);
            await _katalonService.ScanProjectAsync(project);

            project.LastScanned = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project scan completed successfully for ID: {ProjectId}", id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during project scan: {ProjectId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning project: {ProjectId}", id);
            return BadRequest(new { error = "Error scanning project" });
        }
    }
    private string SanitizeProjectName(string name)
    {
        // Trim spaces and replace invalid characters
        return string.Join("_", name.Trim().Split(Path.GetInvalidFileNameChars()));
    }

[HttpPost]
public async Task<IActionResult> SyncProject(int id)
{
    _logger.LogInformation("Starting git sync for project: {ProjectId}", id);

    try
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) {
            _logger.LogWarning("Project not found: {ProjectId}", id);
            return NotFound(new { error = "Project not found" });
        }

        _logger.LogInformation("Found project {Name}, starting git sync at path: {Path}", 
            project.Name, project.GitRepositoryPath);

        await _katalonService.ScanProjectAsync(project);
        
        // Verify directory exists after sync
        if (!Directory.Exists(project.GitRepositoryPath))
        {
            _logger.LogError("Git repository path not found after sync: {Path}", project.GitRepositoryPath);
            return BadRequest(new { error = "Git sync failed - directory not created" });
        }

        _logger.LogInformation("Git sync completed for project: {ProjectName}", project.Name);
        return Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during git sync for project ID: {ProjectId}", id);
        return BadRequest(new { error = ex.Message });
    }
}


    [HttpPost]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _context.Projects
            .Include(p => p.TestSuites)
                .ThenInclude(ts => ts.ScheduledTests)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            return NotFound();

        // Delete all Hangfire jobs associated with this projects test suites
        foreach (var testSuite in project.TestSuites)
        {
            foreach (var scheduledTest in testSuite.ScheduledTests)
            {
                RecurringJob.RemoveIfExists(scheduledTest.JobId);
            }
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetProfiles(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound(new { error = "Project not found" });

        try
        {
            var profilesPath = Path.Combine(project.GitRepositoryPath, "profiles");
            if (!Directory.Exists(profilesPath))
            {
                return Json(new List<object>());
            }

            var profiles = Directory.GetFiles(profilesPath, "*.glbl")
                .Select(path => new
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    path = path.Replace(project.GitRepositoryPath, "").TrimStart('\\', '/')
                })
                .ToList();

            return Json(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for project {Id}", id);
            return BadRequest(new { error = "Error accessing project profiles" });
        }
    }

}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KatalonScheduler.Models.ViewModels;
using KatalonScheduler.Services.Interfaces;
using KatalonScheduler.Data;
using Hangfire;

namespace KatalonScheduler.Controllers;

public class ScheduleController : Controller
{
    private readonly IKatalonService _katalonService;
    private readonly ITestRunnerService _testRunnerService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(
        IKatalonService katalonService,
        ITestRunnerService testRunnerService,
        ApplicationDbContext context,
        ILogger<ScheduleController> logger)
    {
        _katalonService = katalonService;
        _testRunnerService = testRunnerService;
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _katalonService.ScanForProjectsAsync();
        return View(projects);
    }

    public async Task<IActionResult> Create(int projectId)
    {
        var project = await _katalonService.GetProjectAsync(projectId);
        if (project == null)
            return NotFound();

        var testCases = await _katalonService.GetTestCasesAsync(projectId);
        var viewModel = new ScheduleTestViewModel
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TestCases = testCases.ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ScheduleTestViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var project = await _katalonService.GetProjectAsync(model.ProjectId);
            if (project == null)
                return NotFound();

            var testCase = await _context.TestCases.FindAsync(model.TestCaseId);
            if (testCase == null)
                return NotFound();

            var cronExpression = $"{model.Minute} {model.Hour} * * {model.DayOfWeek}";
            RecurringJob.AddOrUpdate(
                $"test-{model.TestCaseId}",
                () => _testRunnerService.RunTestAsync(project.Path, testCase.Path, CancellationToken.None),
                cronExpression);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling test");
            ModelState.AddModelError("", "Error scheduling test. Please try again.");
            return View(model);
        }
    }
}
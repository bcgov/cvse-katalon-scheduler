using KatalonScheduler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KatalonScheduler.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var last24Hours = DateTime.UtcNow.AddHours(-24);
        var recentLogs = await _context.TestExecutionLogs
            .Include(l => l.ScheduledTest)
                .ThenInclude(st => st.TestSuite)
            .Where(l => l.ExecutionTime >= last24Hours)
            .OrderByDescending(l => l.ExecutionTime)
            .ToListAsync() ?? new List<TestExecutionLog>();

        var viewModel = new DashboardViewModel
        {
            SuccessfulTests = recentLogs.Count(l => l.Status == "Success"),
            FailedTests = recentLogs.Count(l => l.Status == "Failed"),
            SchedulerErrors = recentLogs.Count(l => l.Status == "SchedulerError"),
            RuntimeErrors = recentLogs.Count(l => l.Status == "RuntimeError"),
            RecentLogs = recentLogs
        };

        return View(viewModel);
    }

    public IActionResult Error()
    {
        return View();
    }
}
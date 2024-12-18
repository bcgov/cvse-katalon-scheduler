using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KatalonScheduler.Data;
using KatalonScheduler.Models.ViewModels;

namespace KatalonScheduler.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var adminSettings = await _context.AdminSettings.FirstOrDefaultAsync();
        var organizations = await _context.Organizations
        .Include(o => o.Projects)  
        .ToListAsync();

        var viewModel = new AdminViewModel
        {
            Organizations = organizations,
            KatalonPath = adminSettings?.KatalonPath ?? "",
            BaseRepositoryPath = adminSettings?.BaseRepositoryPath ?? ""
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSettings(AdminViewModel model)
    {
        var settings = await _context.AdminSettings.FirstOrDefaultAsync() 
            ?? new AdminSettings();

        settings.KatalonPath = model.KatalonPath;
        settings.BaseRepositoryPath = model.BaseRepositoryPath;
        settings.LastUpdated = DateTime.UtcNow;

        if (settings.Id == 0)
            _context.AdminSettings.Add(settings);
        else
            _context.AdminSettings.Update(settings);

        await _context.SaveChangesAsync();
        TempData["Success"] = "Application settings updated successfully";
        
        return RedirectToAction(nameof(Index));
    }
}
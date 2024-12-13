using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KatalonScheduler.Data;
using KatalonScheduler.Models.Domain;
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

    public async Task<IActionResult> Index()
    {
        var viewModel = new AdminViewModel
        {
            Organizations = await _context.Organizations
                .Include(o => o.Projects)
                .ToListAsync()
        };
        return View(viewModel);
    }
}
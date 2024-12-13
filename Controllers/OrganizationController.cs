using KatalonScheduler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class OrganizationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(ApplicationDbContext context, ILogger<OrganizationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var orgs = await _context.Organizations
            .Include(o => o.Projects)
            .ToListAsync();
        return View(orgs);
    }

    public IActionResult Create()
    {
        // Instead of creating an empty object, we'll just return the view
        // The form will bind to a new object when posted
        return View();
    }

    [HttpPost]
public async Task<IActionResult> Delete(int id)
{
    try
    {
        var org = await _context.Organizations.FindAsync(id);
        if (org == null) return NotFound();

        _context.Organizations.Remove(org);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Organization deleted successfully";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting organization");
        TempData["Error"] = "Error deleting organization";
    }

    return RedirectToAction("Index", "Admin");
}

[HttpPost]
public async Task<IActionResult> Create([FromForm] Organization org)
{
    if (!ModelState.IsValid)
    {
        TempData["Error"] = "Invalid organization data";
        return RedirectToAction("Index", "Admin");
    }

    try
    {
        if (org.Id == 0)
        {
            // Create new
            org.CreatedAt = DateTime.UtcNow;
            _context.Organizations.Add(org);
        }
        else
        {
            // Update existing
            var existing = await _context.Organizations.FindAsync(org.Id);
            if (existing == null) return NotFound();
            
            existing.Name = org.Name;
            existing.KatalonOrganizationId = org.KatalonOrganizationId;
            _context.Organizations.Update(existing);
        }
        
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Organization {(org.Id == 0 ? "created" : "updated")} successfully";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error saving organization");
        TempData["Error"] = "Error saving organization";
    }
    
    return RedirectToAction("Index", "Admin");
}

    [HttpGet]
public async Task<JsonResult> GetOrganization(int id)
{
    var org = await _context.Organizations.FindAsync(id);
    return Json(org);
}

[HttpPost]
public async Task<IActionResult> Edit(int id, Organization org)
{
    if (id != org.Id) return BadRequest();
    
    try
    {
        var existing = await _context.Organizations.FindAsync(id);
        if (existing == null) return NotFound();
        
        existing.Name = org.Name;
        existing.Id = org.Id;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Organization updated successfully";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating organization");
        TempData["Error"] = "Error updating organization";
    }
    
    return RedirectToAction("Index", "Admin");
}
}
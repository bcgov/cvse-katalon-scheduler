using KatalonScheduler.Models.Domain;

namespace KatalonScheduler.Models.ViewModels;

public class AdminViewModel
{
    public List<Organization> Organizations { get; set; } = new();
}
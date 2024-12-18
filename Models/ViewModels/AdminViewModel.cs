
namespace KatalonScheduler.Models.ViewModels;

public class AdminViewModel
{
    public List<Organization> Organizations { get; set; } = new();
    public string KatalonPath { get; set; } = string.Empty;
    public string BaseRepositoryPath { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

public class ProjectViewModel
{
    public string Name { get; set; } = string.Empty;
    public string GitRepositoryPath { get; set; } = string.Empty;
    public string GitUrl { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public long? TestOpsProjectId { get; set; }
}
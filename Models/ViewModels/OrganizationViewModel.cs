using System.ComponentModel.DataAnnotations;

public class OrganizationViewModel
{
    [Required]
    [Display(Name = "Organization Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Short Name")]
    public string ShortName { get; set; } = string.Empty;

    public int TestOpsProjectId { get; set; }
}
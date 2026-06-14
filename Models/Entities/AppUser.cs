using Microsoft.AspNetCore.Identity;

namespace Factors.Web.Models.Entities;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }

    // Navigation
    public virtual ICollection<Factor> CreatedFactors { get; set; } = new List<Factor>();
}

public class AppRole : IdentityRole<int>
{
    public string Description { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}

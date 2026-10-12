using Microsoft.AspNetCore.Identity;

namespace Healthcare_Industry.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? SavedDemoCardholderName { get; set; }
    public string? SavedDemoCardLastFour { get; set; }
    public string? SavedDemoCardExpiry { get; set; }
}

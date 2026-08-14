using Microsoft.AspNetCore.Identity;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Extended ApplicationUser representing system users across 4 primary roles: Admin, Vendor, Visitor, Employee
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation collections
    public virtual ICollection<StallBooking> StallBookings { get; set; } = new List<StallBooking>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
}

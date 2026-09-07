using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Fair staff employment opportunity posted by Fair Administration
/// </summary>
public class JobPosting
{
    [Key]
    public int JobPostingId { get; set; }

    [Required]
    public int FairId { get; set; }

    // Null preserves legacy fair-admin postings; vendor postings are always owned.
    public string? VendorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Department { get; set; } = "Operations";

    public string Description { get; set; } = string.Empty;

    public string Requirements { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DailyWage { get; set; }

    public int PositionsAvailable { get; set; } = 1;

    public int PositionsFilled { get; set; } = 0;

    public DateTime ApplicationDeadline { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(FairId))]
    public virtual Fair Fair { get; set; } = null!;

    [ForeignKey(nameof(VendorId))]
    public virtual ApplicationUser? Vendor { get; set; }

    public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}

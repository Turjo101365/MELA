using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MelaFair.Core.Enums;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Staff job application submitted by an Employee candidate
/// </summary>
public class JobApplication
{
    [Key]
    public int JobApplicationId { get; set; }

    [Required]
    public int JobPostingId { get; set; }

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    [MaxLength(1000)]
    public string ResumeSummary { get; set; } = string.Empty;

    public int ExperienceYears { get; set; } = 0;

    [MaxLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    // Navigation
    [ForeignKey(nameof(JobPostingId))]
    public virtual JobPosting JobPosting { get; set; } = null!;

    [ForeignKey(nameof(EmployeeId))]
    public virtual ApplicationUser Employee { get; set; } = null!;
}

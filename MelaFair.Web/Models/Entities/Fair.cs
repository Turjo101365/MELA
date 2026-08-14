using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Fair master entity representing a cultural festival or exhibition
/// </summary>
public class Fair
{
    [Key]
    public int FairId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    public int DailyCapacity { get; set; }

    public int TotalStalls { get; set; }

    public int AvailableStalls { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseStallPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseTicketPrice { get; set; }

    [MaxLength(500)]
    public string BannerImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<FairDay> FairDays { get; set; } = new List<FairDay>();
    public virtual ICollection<Stall> Stalls { get; set; } = new List<Stall>();
    public virtual ICollection<StallBooking> StallBookings { get; set; } = new List<StallBooking>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
}

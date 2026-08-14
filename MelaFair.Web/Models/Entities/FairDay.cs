using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Represents individual operating days of a fair with daily admission quota
/// </summary>
public class FairDay
{
    [Key]
    public int FairDayId { get; set; }

    [Required]
    public int FairId { get; set; }

    [Column(TypeName = "date")]
    public DateTime Date { get; set; }

    public int DailyCapacity { get; set; }

    public int TicketsSold { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey(nameof(FairId))]
    public virtual Fair Fair { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MelaFair.Core.Enums;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Stall physical unit inside a fair
/// </summary>
public class Stall
{
    [Key]
    public int StallId { get; set; }

    [Required]
    public int FairId { get; set; }

    [Required]
    [MaxLength(50)]
    public string StallNumber { get; set; } = string.Empty;

    public StallCategory Category { get; set; } = StallCategory.General;

    public StallSize Size { get; set; } = StallSize.Medium;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public bool IsBooked { get; set; } = false;

    // Timestamp for optimistic concurrency check
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation
    [ForeignKey(nameof(FairId))]
    public virtual Fair Fair { get; set; } = null!;

    public virtual ICollection<StallBooking> Bookings { get; set; } = new List<StallBooking>();
}

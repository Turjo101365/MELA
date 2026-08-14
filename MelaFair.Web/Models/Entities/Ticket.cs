using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MelaFair.Core.Enums;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Admission ticket purchased by a Visitor for a specific FairDay
/// </summary>
public class Ticket
{
    [Key]
    public int TicketId { get; set; }

    [Required]
    public int FairId { get; set; }

    [Required]
    public int FairDayId { get; set; }

    [Required]
    public string VisitorId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TicketCode { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePaid { get; set; }

    public int Quantity { get; set; } = 1;

    public TicketStatus Status { get; set; } = TicketStatus.Valid;

    // Navigation
    [ForeignKey(nameof(FairId))]
    public virtual Fair Fair { get; set; } = null!;

    [ForeignKey(nameof(FairDayId))]
    public virtual FairDay FairDay { get; set; } = null!;

    [ForeignKey(nameof(VisitorId))]
    public virtual ApplicationUser Visitor { get; set; } = null!;
}

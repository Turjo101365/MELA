using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MelaFair.Core.Enums;

namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Transactional booking record linking a Vendor to a leased Stall
/// </summary>
public class StallBooking
{
    [Key]
    public int StallBookingId { get; set; }

    [Required]
    public int StallId { get; set; }

    [Required]
    public int FairId { get; set; }

    [Required]
    public string VendorId { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;

    [MaxLength(100)]
    public string TransactionReference { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    // Navigation
    [ForeignKey(nameof(StallId))]
    public virtual Stall Stall { get; set; } = null!;

    [ForeignKey(nameof(FairId))]
    public virtual Fair Fair { get; set; } = null!;

    [ForeignKey(nameof(VendorId))]
    public virtual ApplicationUser Vendor { get; set; } = null!;
}

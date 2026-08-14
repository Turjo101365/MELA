using System.ComponentModel.DataAnnotations;
using MelaFair.Core.Enums;
using MelaFair.Web.Models.Entities;

namespace MelaFair.Web.Models.ViewModels;

/// <summary>
/// View model for creating a new cultural fair with stalls and operating schedule
/// </summary>
public class CreateFairViewModel
{
    [Required(ErrorMessage = "Fair title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7);

    [Required(ErrorMessage = "End date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(14);

    [Required(ErrorMessage = "Daily capacity is required")]
    [Range(10, 100000, ErrorMessage = "Daily capacity must be between 10 and 100,000")]
    [Display(Name = "Daily Visitor Capacity")]
    public int DailyCapacity { get; set; } = 5000;

    [Required(ErrorMessage = "Stall count is required")]
    [Range(1, 1000, ErrorMessage = "Stall count must be between 1 and 1,000")]
    [Display(Name = "Total Stalls to Generate")]
    public int TotalStalls { get; set; } = 50;

    [Required(ErrorMessage = "Base stall price is required")]
    [Range(1, 1000000, ErrorMessage = "Base stall lease price must be greater than 0")]
    [Display(Name = "Base Stall Lease Price (BDT/USD)")]
    public decimal BaseStallPrice { get; set; } = 1500;

    [Required(ErrorMessage = "Base ticket price is required")]
    [Range(0, 10000, ErrorMessage = "Ticket price must be positive")]
    [Display(Name = "Admission Ticket Price")]
    public decimal BaseTicketPrice { get; set; } = 50;

    [Display(Name = "Banner Image URL")]
    public string? BannerImageUrl { get; set; }
}

/// <summary>
/// View model for Vendor stall booking checkout
/// </summary>
public class StallBookingViewModel
{
    public int FairId { get; set; }
    public string FairTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal BaseStallPrice { get; set; }

    [Required(ErrorMessage = "Please select at least one stall to book")]
    public List<int> SelectedStallIds { get; set; } = new();

    public string? Notes { get; set; }

    // Display listings
    public List<StallItemDto> AvailableStalls { get; set; } = new();
    public List<StallItemDto> AllStalls { get; set; } = new();
}

public class StallItemDto
{
    public int StallId { get; set; }
    public string StallNumber { get; set; } = string.Empty;
    public StallCategory Category { get; set; }
    public StallSize Size { get; set; }
    public decimal Price { get; set; }
    public bool IsBooked { get; set; }
    public string? BookedBy { get; set; }
}

/// <summary>
/// View model for Vendor's booked stalls list
/// </summary>
public class MyStallsViewModel
{
    public List<StallBookingItemDto> Bookings { get; set; } = new();
}

public class StallBookingItemDto
{
    public int BookingId { get; set; }
    public int StallId { get; set; }
    public string StallNumber { get; set; } = string.Empty;
    public string FairTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public StallCategory Category { get; set; }
    public StallSize Size { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime BookingDate { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
}

/// <summary>
/// View model for Visitor ticket purchase
/// </summary>
public class TicketPurchaseViewModel
{
    public int FairId { get; set; }
    public string FairTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal BaseTicketPrice { get; set; }

    [Required(ErrorMessage = "Please select a visit date")]
    public int FairDayId { get; set; }

    [Required(ErrorMessage = "Please specify number of tickets")]
    [Range(1, 50, ErrorMessage = "Group ticket bookings can be between 1 and 50 tickets")]
    public int Quantity { get; set; } = 1;

    public List<FairDayOptionDto> AvailableDays { get; set; } = new();
}

public class FairDayOptionDto
{
    public int FairDayId { get; set; }
    public DateTime Date { get; set; }
    public int DailyCapacity { get; set; }
    public int TicketsSold { get; set; }
    public int RemainingCapacity => Math.Max(0, DailyCapacity - TicketsSold);
    public bool IsSoldOut => RemainingCapacity <= 0;
}

/// <summary>
/// View model for Visitor's tickets list
/// </summary>
public class MyTicketsViewModel
{
    public List<VisitorTicketItemDto> Tickets { get; set; } = new();
}

public class VisitorTicketItemDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string FairTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal PricePaid { get; set; }
    public int Quantity { get; set; }
    public TicketStatus Status { get; set; }
}

/// <summary>
/// View model for Employee job applications
/// </summary>
public class ApplyJobViewModel
{
    public int JobPostingId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string FairTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal DailyWage { get; set; }
    public string Requirements { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please summarize your skills and experience")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Resume summary must be at least 10 characters")]
    public string ResumeSummary { get; set; } = string.Empty;

    [Range(0, 40, ErrorMessage = "Experience must be between 0 and 40 years")]
    public int ExperienceYears { get; set; } = 0;

    [Required(ErrorMessage = "Contact phone number is required")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string ContactPhone { get; set; } = string.Empty;
}

/// <summary>
/// View model for Employee's submitted applications
/// </summary>
public class MyApplicationsViewModel
{
    public List<JobApplicationItemDto> Applications { get; set; } = new();
}

public class JobApplicationItemDto
{
    public int ApplicationId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string FairTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal DailyWage { get; set; }
    public DateTime ApplicationDate { get; set; }
    public ApplicationStatus Status { get; set; }
    public string ResumeSummary { get; set; } = string.Empty;
}

/// <summary>
/// Mapped to SQL View: vw_FairSummary
/// </summary>
public class FairSummaryViewModel
{
    public int FairId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int TotalStalls { get; set; }
    public int BookedStalls { get; set; }
    public int AvailableStalls { get; set; }
    public decimal StallOccupancyRate { get; set; }
    public int TotalOperatingDays { get; set; }
    public int TotalTicketCapacity { get; set; }
    public int TotalTicketsSold { get; set; }
    public decimal TicketCapacitySoldPercentage { get; set; }
    public decimal StallRevenue { get; set; }
    public decimal TicketRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveJobPostings { get; set; }
}

/// <summary>
/// Mapped to SQL View: vw_DailyVisitorCount
/// </summary>
public class DailyVisitorCountViewModel
{
    public int FairId { get; set; }
    public string FairTitle { get; set; } = string.Empty;
    public DateTime FairDate { get; set; }
    public int DailyCapacity { get; set; }
    public int TicketsSold { get; set; }
    public int RemainingCapacity { get; set; }
    public decimal CapacityUtilizationPercent { get; set; }
    public decimal DailyTicketRevenue { get; set; }
}

/// <summary>
/// Admin aggregated dashboard view model
/// </summary>
public class AdminDashboardViewModel
{
    public int TotalFairs { get; set; }
    public int ActiveFairs { get; set; }
    public int TotalStalls { get; set; }
    public int TotalStallsBooked { get; set; }
    public int TotalTicketsSold { get; set; }
    public decimal TotalStallRevenue { get; set; }
    public decimal TotalTicketRevenue { get; set; }
    public decimal TotalRevenue => TotalStallRevenue + TotalTicketRevenue;
    public int TotalJobApplications { get; set; }

    public List<FairSummaryViewModel> FairSummaries { get; set; } = new();
    public List<DailyVisitorCountViewModel> DailyVisitorAnalytics { get; set; } = new();
}

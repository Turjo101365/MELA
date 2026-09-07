using MelaFair.Core.Enums;

namespace MelaFair.Web.Models.ViewModels;

public class AdminStallsViewModel
{
    public int? SelectedFairId { get; set; }
    public string? SelectedStatus { get; set; } // "all", "booked", "available"
    public string? SearchQuery { get; set; }
    public List<FairOptionDto> Fairs { get; set; } = new();

    public int TotalStalls { get; set; }
    public int BookedStalls { get; set; }
    public int AvailableStalls { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal TotalRevenue { get; set; }

    public List<AdminStallItemDto> StallItems { get; set; } = new();
}

public class AdminStallItemDto
{
    public int StallId { get; set; }
    public int FairId { get; set; }
    public string FairTitle { get; set; } = string.Empty;
    public string StallNumber { get; set; } = string.Empty;
    public StallCategory Category { get; set; }
    public StallSize Size { get; set; }
    public decimal Price { get; set; }
    public bool IsBooked { get; set; }

    public int? BookingId { get; set; }
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? VendorEmail { get; set; }
    public DateTime? BookingDate { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? TransactionReference { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? Notes { get; set; }
}

public class AdminPassesViewModel
{
    public int? SelectedFairId { get; set; }
    public string? SearchQuery { get; set; }
    public List<FairOptionDto> Fairs { get; set; } = new();

    public int TotalTicketsSold { get; set; }
    public decimal TotalTicketRevenue { get; set; }
    public int TotalFairCapacity { get; set; }
    public decimal OverallUtilizationPercent { get; set; }
    public int TotalOperatingDays { get; set; }

    public List<AdminPassFairDayDto> OperatingDays { get; set; } = new();
    public List<AdminTicketItemDto> RecentTickets { get; set; } = new();
}

public class AdminPassFairDayDto
{
    public int FairDayId { get; set; }
    public int FairId { get; set; }
    public string FairTitle { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string OperatingHours { get; set; } = "10:00 AM - 10:00 PM";
    public int DailyCapacity { get; set; }
    public int TicketsSold { get; set; }
    public int AvailableCapacity => Math.Max(0, DailyCapacity - TicketsSold);
    public decimal UtilizationPercent => DailyCapacity > 0 ? Math.Round(((decimal)TicketsSold / DailyCapacity) * 100m, 1) : 0m;
    public decimal BaseTicketPrice { get; set; }
    public decimal Revenue => TicketsSold * BaseTicketPrice;
}

public class AdminTicketItemDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public int FairId { get; set; }
    public string FairTitle { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string VisitorId { get; set; } = string.Empty;
    public string VisitorName { get; set; } = string.Empty;
    public string VisitorEmail { get; set; } = string.Empty;
    public decimal PricePaid { get; set; }
    public DateTime PurchaseDate { get; set; }
}

public class AdminStaffJobsViewModel
{
    public int? SelectedFairId { get; set; }
    public string? SelectedDepartment { get; set; }
    public string? SearchQuery { get; set; }
    public List<FairOptionDto> Fairs { get; set; } = new();
    public List<string> Departments { get; set; } = new();

    public int TotalJobPostings { get; set; }
    public int ActiveJobs { get; set; }
    public int TotalPositionsAvailable { get; set; }
    public int TotalPositionsFilled { get; set; }
    public int TotalApplicationsReceived { get; set; }
    public int PendingApplications { get; set; }

    public List<AdminJobPostingDto> JobPostings { get; set; } = new();
    public List<AdminJobApplicationDto> Applications { get; set; } = new();
}

public class AdminJobPostingDto
{
    public int JobPostingId { get; set; }
    public int FairId { get; set; }
    public string FairTitle { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string VendorEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public decimal DailyWage { get; set; }
    public int PositionsAvailable { get; set; }
    public int PositionsFilled { get; set; }
    public DateTime ApplicationDeadline { get; set; }
    public bool IsActive { get; set; }
    public int ApplicationsCount { get; set; }
    public int PendingApplicationsCount { get; set; }
}

public class AdminJobApplicationDto
{
    public int JobApplicationId { get; set; }
    public int JobPostingId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string FairTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ResumeSummary { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public DateTime ApplicationDate { get; set; }
    public ApplicationStatus Status { get; set; }
}

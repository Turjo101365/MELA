using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MelaFair.Core.Constants;
using MelaFair.Core.Enums;
using MelaFair.Web.Data;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Services;

namespace MelaFair.Web.Controllers;

/// <summary>
/// Controller for Fair Administrators to provision fairs, inspect analytics views, and manage events
/// </summary>
[Authorize(Roles = RoleConstants.Admin)]
public class AdminController : Controller
{
    private readonly FairService _fairService;
    private readonly ApplicationDbContext _dbContext;
    private readonly StallBookingService _stallBookingService;
    private readonly RecruitmentService _recruitmentService;

    public AdminController(
        FairService fairService,
        ApplicationDbContext dbContext,
        StallBookingService stallBookingService,
        RecruitmentService recruitmentService)
    {
        _fairService = fairService;
        _dbContext = dbContext;
        _stallBookingService = stallBookingService;
        _recruitmentService = recruitmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var dashboardData = await _fairService.GetAdminDashboardDataAsync();
        return View(dashboardData);
    }

    [HttpGet]
    public IActionResult CreateFair()
    {
        return View(new CreateFairViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFair(CreateFairViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, fairId, message) = await _fairService.CreateFairAsync(model);
        if (success)
        {
            TempData["SuccessMessage"] = $"{message} (Fair #{fairId}: {model.Title})";
            return RedirectToAction("ManageFairs");
        }

        ModelState.AddModelError(string.Empty, message);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ManageFairs()
    {
        var fairs = await _fairService.GetAllFairsAsync();
        return View(fairs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFair(int id)
    {
        bool deleted = await _fairService.DeleteFairAsync(id);
        if (deleted)
        {
            TempData["SuccessMessage"] = "Fair and its associated operating days, stalls, and tickets were successfully deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete fair. Fair not found.";
        }

        return RedirectToAction("ManageFairs");
    }

    [HttpGet]
    public async Task<IActionResult> Analytics(int? fairId = null)
    {
        var summaries = await _fairService.GetFairSummariesAsync();
        var dailyCounts = await _fairService.GetDailyVisitorCountsAsync(fairId);

        ViewBag.SelectedFairId = fairId;
        ViewBag.FairSummaries = summaries;
        ViewBag.AllFairs = await _fairService.GetAllFairsAsync();

        return View(dailyCounts);
    }

    [HttpGet]
    public async Task<IActionResult> Stalls(int? fairId = null, string? status = null, string? search = null)
    {
        var allFairs = (await _fairService.GetAllFairsAsync()).ToList();

        var query = _dbContext.Stalls
            .Include(s => s.Fair)
            .Include(s => s.Bookings.Where(b => b.PaymentStatus != PaymentStatus.Refunded))
                .ThenInclude(b => b.Vendor)
            .AsQueryable();

        if (fairId.HasValue && fairId.Value > 0)
        {
            query = query.Where(s => s.FairId == fairId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("booked", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => s.IsBooked);
            else if (status.Equals("available", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => !s.IsBooked);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.StallNumber.Contains(search) || 
                                     (s.Fair != null && s.Fair.Title.Contains(search)) ||
                                     s.Bookings.Any(b => (b.Vendor != null && b.Vendor.FullName.Contains(search)) || 
                                                         (b.Vendor != null && b.Vendor.Email != null && b.Vendor.Email.Contains(search))));
        }

        var stallEntities = await query.OrderBy(s => s.Fair.Title).ThenBy(s => s.StallNumber).ToListAsync();

        var items = stallEntities.Select(s => {
            var activeBooking = s.Bookings.OrderByDescending(b => b.BookingDate).FirstOrDefault();
            return new AdminStallItemDto
            {
                StallId = s.StallId,
                FairId = s.FairId,
                FairTitle = s.Fair?.Title ?? "N/A",
                StallNumber = s.StallNumber,
                Category = s.Category,
                Size = s.Size,
                Price = s.Price,
                IsBooked = s.IsBooked,
                BookingId = activeBooking?.StallBookingId,
                VendorId = activeBooking?.VendorId,
                VendorName = activeBooking?.Vendor?.FullName ?? (s.IsBooked ? "Reserved Vendor" : null),
                VendorEmail = activeBooking?.Vendor?.Email,
                BookingDate = activeBooking?.BookingDate,
                AmountPaid = activeBooking?.AmountPaid,
                TransactionReference = activeBooking?.TransactionReference,
                PaymentStatus = activeBooking?.PaymentStatus,
                Notes = activeBooking?.Notes
            };
        }).ToList();

        var baseQuery = _dbContext.Stalls.AsQueryable();
        if (fairId.HasValue && fairId.Value > 0) baseQuery = baseQuery.Where(s => s.FairId == fairId.Value);

        var totalStalls = await baseQuery.CountAsync();
        var bookedStalls = await baseQuery.CountAsync(s => s.IsBooked);
        var totalRev = await _dbContext.StallBookings
            .Where(b => b.PaymentStatus == PaymentStatus.Paid && (!fairId.HasValue || b.FairId == fairId.Value))
            .SumAsync(b => (decimal?)b.AmountPaid) ?? 0m;

        var vm = new AdminStallsViewModel
        {
            SelectedFairId = fairId,
            SelectedStatus = status ?? "all",
            SearchQuery = search,
            Fairs = allFairs.Select(f => new FairOptionDto { FairId = f.FairId, Title = f.Title }).ToList(),
            TotalStalls = totalStalls,
            BookedStalls = bookedStalls,
            AvailableStalls = Math.Max(0, totalStalls - bookedStalls),
            OccupancyRate = totalStalls > 0 ? Math.Round(((decimal)bookedStalls / totalStalls) * 100m, 1) : 0m,
            TotalRevenue = totalRev,
            StallItems = items
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelStallBooking(int bookingId, int? fairId = null)
    {
        var (success, message) = await _stallBookingService.CancelBookingAsync(bookingId, string.Empty, isAdmin: true);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Stalls), new { fairId });
    }

    [HttpGet]
    public async Task<IActionResult> Passes(int? fairId = null, string? search = null)
    {
        var allFairs = (await _fairService.GetAllFairsAsync()).ToList();

        var daysQuery = _dbContext.FairDays
            .Include(fd => fd.Fair)
            .AsQueryable();

        if (fairId.HasValue && fairId.Value > 0)
        {
            daysQuery = daysQuery.Where(fd => fd.FairId == fairId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            daysQuery = daysQuery.Where(fd => fd.Fair.Title.Contains(search));
        }

        var days = await daysQuery.OrderBy(fd => fd.Date).ToListAsync();

        var operatingDaysDtos = days.Select(d => new AdminPassFairDayDto
        {
            FairDayId = d.FairDayId,
            FairId = d.FairId,
            FairTitle = d.Fair?.Title ?? "N/A",
            Date = d.Date,
            DailyCapacity = d.DailyCapacity,
            TicketsSold = d.TicketsSold,
            BaseTicketPrice = d.Fair?.BaseTicketPrice ?? 0m
        }).ToList();

        var ticketsQuery = _dbContext.Tickets
            .Include(t => t.Fair)
            .Include(t => t.FairDay)
            .Include(t => t.Visitor)
            .AsQueryable();

        if (fairId.HasValue && fairId.Value > 0)
        {
            ticketsQuery = ticketsQuery.Where(t => t.FairId == fairId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            ticketsQuery = ticketsQuery.Where(t => t.TicketCode.Contains(search) || 
                                                  (t.Visitor != null && t.Visitor.FullName.Contains(search)) || 
                                                  (t.Visitor != null && t.Visitor.Email != null && t.Visitor.Email.Contains(search)) ||
                                                  (t.Fair != null && t.Fair.Title.Contains(search)));
        }

        var ticketsList = await ticketsQuery
            .OrderByDescending(t => t.PurchaseDate)
            .Take(50)
            .ToListAsync();

        var recentTickets = ticketsList.Select(t => new AdminTicketItemDto
        {
            TicketId = t.TicketId,
            TicketCode = t.TicketCode,
            FairId = t.FairId,
            FairTitle = t.Fair?.Title ?? "N/A",
            VisitDate = t.FairDay?.Date ?? DateTime.MinValue,
            VisitorId = t.VisitorId,
            VisitorName = t.Visitor?.FullName ?? "Attendee",
            VisitorEmail = t.Visitor?.Email ?? string.Empty,
            PricePaid = t.PricePaid,
            PurchaseDate = t.PurchaseDate
        }).ToList();

        int totalCap = operatingDaysDtos.Sum(d => d.DailyCapacity);
        int totalSold = operatingDaysDtos.Sum(d => d.TicketsSold);
        decimal totalRev = await _dbContext.Tickets
            .Where(t => !fairId.HasValue || t.FairId == fairId.Value)
            .SumAsync(t => (decimal?)t.PricePaid) ?? 0m;

        var vm = new AdminPassesViewModel
        {
            SelectedFairId = fairId,
            SearchQuery = search,
            Fairs = allFairs.Select(f => new FairOptionDto { FairId = f.FairId, Title = f.Title }).ToList(),
            TotalTicketsSold = totalSold,
            TotalTicketRevenue = totalRev,
            TotalFairCapacity = totalCap,
            OverallUtilizationPercent = totalCap > 0 ? Math.Round(((decimal)totalSold / totalCap) * 100m, 1) : 0m,
            TotalOperatingDays = operatingDaysDtos.Count,
            OperatingDays = operatingDaysDtos,
            RecentTickets = recentTickets
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> StaffJobs(int? fairId = null, string? department = null, string? search = null)
    {
        var allFairs = (await _fairService.GetAllFairsAsync()).ToList();

        var query = _dbContext.JobPostings
            .Include(j => j.Fair)
            .Include(j => j.Vendor)
            .Include(j => j.Applications)
                .ThenInclude(a => a.Employee)
            .AsQueryable();

        if (fairId.HasValue && fairId.Value > 0)
        {
            query = query.Where(j => j.FairId == fairId.Value);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(j => j.Department == department);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(j => j.Title.Contains(search) || 
                                     j.Description.Contains(search) || 
                                     (j.Fair != null && j.Fair.Title.Contains(search)) || 
                                     (j.Vendor != null && j.Vendor.FullName.Contains(search)));
        }

        var postings = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

        var depts = await _dbContext.JobPostings.Select(j => j.Department).Distinct().ToListAsync();

        var postingDtos = postings.Select(j => new AdminJobPostingDto
        {
            JobPostingId = j.JobPostingId,
            FairId = j.FairId,
            FairTitle = j.Fair?.Title ?? "N/A",
            VendorId = j.VendorId ?? string.Empty,
            VendorName = j.Vendor?.FullName ?? "Host Vendor",
            VendorEmail = j.Vendor?.Email ?? string.Empty,
            Title = j.Title,
            Department = j.Department,
            Description = j.Description,
            Requirements = j.Requirements,
            DailyWage = j.DailyWage,
            PositionsAvailable = j.PositionsAvailable,
            PositionsFilled = j.PositionsFilled,
            ApplicationDeadline = j.ApplicationDeadline,
            IsActive = j.IsActive,
            ApplicationsCount = j.Applications.Count,
            PendingApplicationsCount = j.Applications.Count(a => a.Status == ApplicationStatus.Pending)
        }).ToList();

        var allApplications = postings.SelectMany(j => j.Applications).Select(a => new AdminJobApplicationDto
        {
            JobApplicationId = a.JobApplicationId,
            JobPostingId = a.JobPostingId,
            JobTitle = a.JobPosting?.Title ?? "N/A",
            FairTitle = a.JobPosting?.Fair?.Title ?? "N/A",
            Department = a.JobPosting?.Department ?? "General",
            ApplicantName = a.Employee?.FullName ?? "Applicant",
            ApplicantEmail = a.Employee?.Email ?? string.Empty,
            ContactPhone = a.ContactPhone,
            ResumeSummary = a.ResumeSummary,
            ExperienceYears = a.ExperienceYears,
            ApplicationDate = a.ApplicationDate,
            Status = a.Status
        }).OrderByDescending(a => a.ApplicationDate).ToList();

        var vm = new AdminStaffJobsViewModel
        {
            SelectedFairId = fairId,
            SelectedDepartment = department,
            SearchQuery = search,
            Fairs = allFairs.Select(f => new FairOptionDto { FairId = f.FairId, Title = f.Title }).ToList(),
            Departments = depts,
            TotalJobPostings = postingDtos.Count,
            ActiveJobs = postingDtos.Count(j => j.IsActive),
            TotalPositionsAvailable = postingDtos.Sum(j => j.PositionsAvailable),
            TotalPositionsFilled = postingDtos.Sum(j => j.PositionsFilled),
            TotalApplicationsReceived = allApplications.Count,
            PendingApplications = allApplications.Count(a => a.Status == ApplicationStatus.Pending),
            JobPostings = postingDtos,
            Applications = allApplications
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewJobApplication(int applicationId, ApplicationStatus status, int? fairId = null)
    {
        var application = await _dbContext.JobApplications
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.JobApplicationId == applicationId);

        if (application == null)
        {
            TempData["ErrorMessage"] = "Job application not found.";
            return RedirectToAction(nameof(StaffJobs), new { fairId });
        }

        if (status != ApplicationStatus.Accepted && status != ApplicationStatus.Rejected)
        {
            TempData["ErrorMessage"] = "Please select a valid status (Accepted or Rejected).";
            return RedirectToAction(nameof(StaffJobs), new { fairId });
        }

        if (status == ApplicationStatus.Accepted && application.JobPosting.PositionsFilled >= application.JobPosting.PositionsAvailable)
        {
            TempData["ErrorMessage"] = "All positions have already been filled for this job role.";
            return RedirectToAction(nameof(StaffJobs), new { fairId });
        }

        var previousStatus = application.Status;
        application.Status = status;

        if (status == ApplicationStatus.Accepted && previousStatus != ApplicationStatus.Accepted)
        {
            application.JobPosting.PositionsFilled++;
        }
        else if (status == ApplicationStatus.Rejected && previousStatus == ApplicationStatus.Accepted)
        {
            application.JobPosting.PositionsFilled = Math.Max(0, application.JobPosting.PositionsFilled - 1);
        }

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Application #{applicationId} for '{application.JobPosting.Title}' has been marked as {status}.";
        return RedirectToAction(nameof(StaffJobs), new { fairId });
    }
}

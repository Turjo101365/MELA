using Microsoft.Data.SqlClient;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Services;

/// <summary>
/// Service managing Vendor stall selections and atomic booking checkout
/// </summary>
public class StallBookingService
{
    private readonly IFairRepository _fairRepository;
    private readonly IStallRepository _stallRepository;
    private readonly ILogger<StallBookingService> _logger;

    public StallBookingService(
        IFairRepository fairRepository,
        IStallRepository stallRepository,
        ILogger<StallBookingService> logger)
    {
        _fairRepository = fairRepository;
        _stallRepository = stallRepository;
        _logger = logger;
    }

    public async Task<StallBookingViewModel?> GetStallBookingViewDataAsync(int fairId)
    {
        var fair = await _fairRepository.GetFairByIdAsync(fairId);
        if (fair == null) return null;

        var stalls = await _stallRepository.GetStallsByFairIdAsync(fairId);

        var vm = new StallBookingViewModel
        {
            FairId = fair.FairId,
            FairTitle = fair.Title,
            Location = fair.Location,
            StartDate = fair.StartDate,
            EndDate = fair.EndDate,
            BaseStallPrice = fair.BaseStallPrice,
            AllStalls = stalls.Select(s => new StallItemDto
            {
                StallId = s.StallId,
                StallNumber = s.StallNumber,
                Category = s.Category,
                Size = s.Size,
                Price = s.Price,
                IsBooked = s.IsBooked
            }).ToList(),
            AvailableStalls = stalls.Where(s => !s.IsBooked).Select(s => new StallItemDto
            {
                StallId = s.StallId,
                StallNumber = s.StallNumber,
                Category = s.Category,
                Size = s.Size,
                Price = s.Price,
                IsBooked = s.IsBooked
            }).ToList()
        };

        return vm;
    }

    public async Task<(bool success, List<int> bookedIds, string message)> BookStallsAsync(
        int fairId, 
        List<int> stallIds, 
        string vendorId, 
        string? notes)
    {
        if (stallIds == null || !stallIds.Any())
        {
            return (false, new List<int>(), "Please select at least one stall to book.");
        }

        var successfulBookingIds = new List<int>();

        foreach (var stallId in stallIds)
        {
            try
            {
                int bookingId = await _stallRepository.BuyStallViaSpAsync(fairId, stallId, vendorId, notes);
                successfulBookingIds.Add(bookingId);
            }
            catch (SqlException ex) when (ex.Number == SqlErrorCodes.StallAlreadyBooked)
            {
                _logger.LogWarning("Stall {StallId} was already booked during concurrent attempt.", stallId);
                return (false, successfulBookingIds, "One or more of the selected stalls were just booked by another vendor. Please refresh and select again.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error booking stall {StallId}.", stallId);
                return (false, successfulBookingIds, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error booking stall {StallId}.", stallId);
                return (false, successfulBookingIds, "An unexpected error occurred during checkout. Please try again.");
            }
        }

        return (true, successfulBookingIds, $"Successfully reserved {successfulBookingIds.Count} stall(s)!");
    }

    public async Task<MyStallsViewModel> GetVendorStallsAsync(string vendorId)
    {
        var bookings = await _stallRepository.GetVendorBookingsAsync(vendorId);

        var vm = new MyStallsViewModel
        {
            Bookings = bookings.Select(b => new StallBookingItemDto
            {
                BookingId = b.StallBookingId,
                StallId = b.StallId,
                StallNumber = b.Stall?.StallNumber ?? "N/A",
                FairTitle = b.Fair?.Title ?? "N/A",
                Location = b.Fair?.Location ?? "N/A",
                StartDate = b.Fair?.StartDate ?? DateTime.MinValue,
                EndDate = b.Fair?.EndDate ?? DateTime.MinValue,
                Category = b.Stall?.Category ?? Core.Enums.StallCategory.General,
                Size = b.Stall?.Size ?? Core.Enums.StallSize.Medium,
                AmountPaid = b.AmountPaid,
                BookingDate = b.BookingDate,
                PaymentStatus = b.PaymentStatus,
                TransactionReference = b.TransactionReference,
                Notes = b.Notes
            }).ToList()
        };

        return vm;
    }

    public async Task<VendorDashboardViewModel> GetVendorDashboardDataAsync(string vendorId, string vendorName, string vendorEmail)
    {
        var bookings = (await _stallRepository.GetVendorBookingsAsync(vendorId)).ToList();
        var activeFairs = (await _fairRepository.GetAllFairsAsync(onlyActive: true)).ToList();

        var bookingDtos = bookings.Select(b => new StallBookingItemDto
        {
            BookingId = b.StallBookingId,
            StallId = b.StallId,
            StallNumber = b.Stall?.StallNumber ?? "N/A",
            FairTitle = b.Fair?.Title ?? "N/A",
            Location = b.Fair?.Location ?? "N/A",
            StartDate = b.Fair?.StartDate ?? DateTime.MinValue,
            EndDate = b.Fair?.EndDate ?? DateTime.MinValue,
            Category = b.Stall?.Category ?? Core.Enums.StallCategory.General,
            Size = b.Stall?.Size ?? Core.Enums.StallSize.Medium,
            AmountPaid = b.AmountPaid,
            BookingDate = b.BookingDate,
            PaymentStatus = b.PaymentStatus,
            TransactionReference = b.TransactionReference,
            Notes = b.Notes
        }).ToList();

        var validPaidBookings = bookingDtos.Where(b => b.PaymentStatus == Core.Enums.PaymentStatus.Paid).ToList();
        var upcomingBookings = validPaidBookings.Where(b => b.StartDate >= DateTime.Today).OrderBy(b => b.StartDate).ToList();

        var nextBooking = upcomingBookings.FirstOrDefault();
        int daysUntilNext = 0;
        if (nextBooking != null)
        {
            daysUntilNext = Math.Max(0, (nextBooking.StartDate.Date - DateTime.Today).Days);
        }

        // Category breakdown for Chart.js
        var categoryMap = validPaidBookings
            .GroupBy(b => b.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Spending by Fair for Chart.js
        var spendingMap = validPaidBookings
            .GroupBy(b => b.FairTitle)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.AmountPaid));

        var vm = new VendorDashboardViewModel
        {
            VendorName = vendorName,
            VendorEmail = vendorEmail,
            TotalStallsLeased = validPaidBookings.Count,
            ActiveFairsCount = validPaidBookings.Select(b => b.FairTitle).Distinct().Count(),
            TotalAmountInvested = validPaidBookings.Sum(b => b.AmountPaid),
            UpcomingFairsCount = upcomingBookings.Count,
            NextUpcomingBooking = nextBooking,
            DaysUntilNextFair = daysUntilNext,
            RecentBookings = bookingDtos.Take(5).ToList(),
            RecommendedFairs = activeFairs.Where(f => f.AvailableStalls > 0).Take(3).ToList(),
            CategoryDistribution = categoryMap,
            SpendingByFair = spendingMap
        };

        return vm;
    }

    public async Task<StallReceiptViewModel?> GetStallReceiptAsync(int bookingId, string vendorId, bool isAdmin = false)
    {
        var booking = await _stallRepository.GetBookingByIdAsync(bookingId);
        if (booking == null || (!isAdmin && booking.VendorId != vendorId))
        {
            return null;
        }

        return new StallReceiptViewModel
        {
            BookingId = booking.StallBookingId,
            StallId = booking.StallId,
            TransactionReference = booking.TransactionReference,
            VendorName = booking.Vendor?.FullName ?? "Authorized Vendor",
            VendorEmail = booking.Vendor?.Email ?? string.Empty,
            FairTitle = booking.Fair?.Title ?? "Cultural Fair",
            Location = booking.Fair?.Location ?? "Fair Grounds",
            StartDate = booking.Fair?.StartDate ?? DateTime.MinValue,
            EndDate = booking.Fair?.EndDate ?? DateTime.MinValue,
            StallNumber = booking.Stall?.StallNumber ?? "S-000",
            Category = booking.Stall?.Category ?? Core.Enums.StallCategory.General,
            Size = booking.Stall?.Size ?? Core.Enums.StallSize.Medium,
            AmountPaid = booking.AmountPaid,
            BookingDate = booking.BookingDate,
            PaymentStatus = booking.PaymentStatus,
            Notes = booking.Notes,
            BasePrice = booking.Fair?.BaseStallPrice ?? 0,
            EstimatedDailyFootfall = booking.Fair?.DailyCapacity ?? 5000
        };
    }

    public async Task<(bool success, string message)> CancelBookingAsync(int bookingId, string vendorId, bool isAdmin = false)
    {
        var booking = await _stallRepository.GetBookingByIdAsync(bookingId);
        if (booking == null || (!isAdmin && booking.VendorId != vendorId))
        {
            return (false, "Stall booking record was not found or access is unauthorized.");
        }

        if (!isAdmin && booking.Fair != null && booking.Fair.StartDate <= DateTime.Today)
        {
            return (false, "Reservations cannot be cancelled for fairs that have already commenced or concluded.");
        }

        bool cancelled = await _stallRepository.CancelBookingAsync(bookingId, vendorId, isAdmin);
        if (cancelled)
        {
            return (true, $"Stall {booking.Stall?.StallNumber} reservation has been successfully cancelled and marked for refund.");
        }

        return (false, "Failed to cancel booking. It may have already been refunded.");
    }
}

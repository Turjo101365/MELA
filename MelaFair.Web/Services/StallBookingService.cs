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
                TransactionReference = b.TransactionReference
            }).ToList()
        };

        return vm;
    }
}

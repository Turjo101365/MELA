using MelaFair.Web.Models.Entities;

namespace MelaFair.Web.Repositories.Interfaces;

/// <summary>
/// Repository interface for Stalls and concurrency-safe bookings
/// </summary>
public interface IStallRepository
{
    Task<IEnumerable<Stall>> GetStallsByFairIdAsync(int fairId);
    Task<int> BuyStallViaSpAsync(int fairId, int stallId, string vendorId, string? notes);
    Task<IEnumerable<StallBooking>> GetVendorBookingsAsync(string vendorId);
    Task<StallBooking?> GetBookingByIdAsync(int bookingId);
    Task<IEnumerable<int>> GetVendorFairIdsAsync(string vendorId);
    Task<bool> CancelBookingAsync(int bookingId, string vendorId, bool isAdmin = false);
}

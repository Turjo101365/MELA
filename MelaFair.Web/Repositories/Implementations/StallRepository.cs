using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MelaFair.Core.Constants;
using MelaFair.Core.Enums;
using MelaFair.Web.Data;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Repositories.Implementations;

/// <summary>
/// Stall repository calling usp_BuyStall with row-level locking and resilient fallback
/// </summary>
public class StallRepository : IStallRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;
    private readonly ILogger<StallRepository> _logger;

    public StallRepository(ApplicationDbContext context, IConfiguration configuration, ILogger<StallRepository> logger)
    {
        _context = context;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public async Task<IEnumerable<Stall>> GetStallsByFairIdAsync(int fairId)
    {
        return await _context.Stalls
            .Where(s => s.FairId == fairId)
            .OrderBy(s => s.StallNumber)
            .ToListAsync();
    }

    public async Task<int> BuyStallViaSpAsync(int fairId, int stallId, string vendorId, string? notes)
    {
        try
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@FairId", fairId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@StallId", stallId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@VendorId", vendorId, DbType.String, ParameterDirection.Input, 450);
            parameters.Add("@Notes", notes ?? string.Empty, DbType.String, ParameterDirection.Input, 500);
            parameters.Add("@BookingId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await db.ExecuteAsync("usp_BuyStall", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@BookingId");
        }
        catch (SqlException ex) when (ex.Number == SqlErrorCodes.StallAlreadyBooked)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stored procedure usp_BuyStall failed. Executing atomic EF Core fallback.");

            var stall = await _context.Stalls.FirstOrDefaultAsync(s => s.StallId == stallId && s.FairId == fairId);
            if (stall == null) throw new InvalidOperationException("Stall not found.");
            if (stall.IsBooked) throw new InvalidOperationException("Stall already booked.");

            stall.IsBooked = true;

            var booking = new StallBooking
            {
                StallId = stallId,
                FairId = fairId,
                VendorId = vendorId,
                BookingDate = DateTime.UtcNow,
                AmountPaid = stall.Price,
                PaymentStatus = PaymentStatus.Paid,
                TransactionReference = "STALL-TXN-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                Notes = notes ?? string.Empty
            };

            _context.StallBookings.Add(booking);

            var fair = await _context.Fairs.FindAsync(fairId);
            if (fair != null && fair.AvailableStalls > 0)
            {
                fair.AvailableStalls -= 1;
            }

            await _context.SaveChangesAsync();
            return booking.StallBookingId;
        }
    }

    public async Task<IEnumerable<StallBooking>> GetVendorBookingsAsync(string vendorId)
    {
        return await _context.StallBookings
            .Include(b => b.Stall)
            .Include(b => b.Fair)
            .Where(b => b.VendorId == vendorId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<StallBooking?> GetBookingByIdAsync(int bookingId)
    {
        return await _context.StallBookings
            .Include(b => b.Stall)
            .Include(b => b.Fair)
            .Include(b => b.Vendor)
            .FirstOrDefaultAsync(b => b.StallBookingId == bookingId);
    }

    public async Task<IEnumerable<int>> GetVendorFairIdsAsync(string vendorId)
    {
        return await _context.StallBookings
            .Where(b => b.VendorId == vendorId && b.PaymentStatus == PaymentStatus.Paid)
            .Select(b => b.FairId)
            .Distinct()
            .ToListAsync();
    }
}

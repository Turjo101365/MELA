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
/// Ticket repository executing usp_BuyFairTicket with resilient capacity locking and EF Core fallback
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(ApplicationDbContext context, IConfiguration configuration, ILogger<TicketRepository> logger)
    {
        _context = context;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public async Task<(decimal totalPaid, string ticketCode)> BuyTicketViaSpAsync(int fairId, int fairDayId, string visitorId, int quantity)
    {
        try
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@FairId", fairId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@FairDayId", fairDayId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@VisitorId", visitorId, DbType.String, ParameterDirection.Input, 450);
            parameters.Add("@Quantity", quantity, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@TotalPaid", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            parameters.Add("@TicketCode", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);

            await db.ExecuteAsync("usp_BuyFairTicket", parameters, commandType: CommandType.StoredProcedure);

            decimal totalPaid = parameters.Get<decimal>("@TotalPaid");
            string ticketCode = parameters.Get<string>("@TicketCode");

            return (totalPaid, ticketCode);
        }
        catch (SqlException ex) when (ex.Number == SqlErrorCodes.DailyCapacityExceeded)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stored procedure usp_BuyFairTicket failed. Executing atomic EF Core fallback.");

            var fairDay = await _context.FairDays.FirstOrDefaultAsync(fd => fd.FairDayId == fairDayId && fd.FairId == fairId);
            var fair = await _context.Fairs.FindAsync(fairId);

            if (fairDay == null || fair == null)
            {
                throw new InvalidOperationException("Fair day not found.");
            }

            if ((fairDay.TicketsSold + quantity) > fairDay.DailyCapacity)
            {
                int remaining = Math.Max(0, fairDay.DailyCapacity - fairDay.TicketsSold);
                throw new InvalidOperationException($"Admission capacity exceeded! Only {remaining} ticket(s) remaining for this date.");
            }

            decimal totalPaid = fair.BaseTicketPrice * quantity;
            string ticketCode = $"TKT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}-{fairId}";

            fairDay.TicketsSold += quantity;

            var ticket = new Ticket
            {
                FairId = fairId,
                FairDayId = fairDayId,
                VisitorId = visitorId,
                TicketCode = ticketCode,
                PurchaseDate = DateTime.UtcNow,
                PricePaid = totalPaid,
                Quantity = quantity,
                Status = TicketStatus.Valid
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return (totalPaid, ticketCode);
        }
    }

    public async Task<IEnumerable<FairDay>> GetFairDaysByFairIdAsync(int fairId)
    {
        return await _context.FairDays
            .Where(fd => fd.FairId == fairId && fd.IsActive)
            .OrderBy(fd => fd.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetVisitorTicketsAsync(string visitorId)
    {
        return await _context.Tickets
            .Include(t => t.Fair)
            .Include(t => t.FairDay)
            .Where(t => t.VisitorId == visitorId)
            .OrderByDescending(t => t.PurchaseDate)
            .ToListAsync();
    }

    public async Task<Ticket?> GetTicketByIdAsync(int ticketId)
    {
        return await _context.Tickets
            .Include(t => t.Fair)
            .Include(t => t.FairDay)
            .Include(t => t.Visitor)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);
    }
}

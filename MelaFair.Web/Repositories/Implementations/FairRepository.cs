using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MelaFair.Core.Enums;
using MelaFair.Web.Data;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Repositories.Implementations;

/// <summary>
/// Fair repository implementation calling usp_CreateFair and analytical views with resilient EF Core fallback
/// </summary>
public class FairRepository : IFairRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;
    private readonly ILogger<FairRepository> _logger;

    public FairRepository(ApplicationDbContext context, IConfiguration configuration, ILogger<FairRepository> logger)
    {
        _context = context;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public async Task<int> CreateFairViaSpAsync(CreateFairViewModel model)
    {
        try
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Title", model.Title, DbType.String, ParameterDirection.Input, 200);
            parameters.Add("@Description", model.Description, DbType.String, ParameterDirection.Input);
            parameters.Add("@Location", model.Location, DbType.String, ParameterDirection.Input, 200);
            parameters.Add("@StartDate", model.StartDate, DbType.Date, ParameterDirection.Input);
            parameters.Add("@EndDate", model.EndDate, DbType.Date, ParameterDirection.Input);
            parameters.Add("@DailyCapacity", model.DailyCapacity, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@TotalStalls", model.TotalStalls, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@BaseStallPrice", model.BaseStallPrice, DbType.Decimal, ParameterDirection.Input);
            parameters.Add("@BaseTicketPrice", model.BaseTicketPrice, DbType.Decimal, ParameterDirection.Input);
            parameters.Add("@BannerImageUrl", model.BannerImageUrl ?? string.Empty, DbType.String, ParameterDirection.Input, 500);
            parameters.Add("@FairId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await db.ExecuteAsync("usp_CreateFair", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@FairId");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stored procedure usp_CreateFair failed or was unavailable. Executing transactional EF Core fallback.");

            var fair = new Fair
            {
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                DailyCapacity = model.DailyCapacity,
                TotalStalls = model.TotalStalls,
                AvailableStalls = model.TotalStalls,
                BaseStallPrice = model.BaseStallPrice,
                BaseTicketPrice = model.BaseTicketPrice,
                BannerImageUrl = model.BannerImageUrl ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Fairs.Add(fair);
            await _context.SaveChangesAsync();

            // Generate FairDays
            var days = new List<FairDay>();
            for (var d = model.StartDate; d <= model.EndDate; d = d.AddDays(1))
            {
                days.Add(new FairDay
                {
                    FairId = fair.FairId,
                    Date = d,
                    DailyCapacity = model.DailyCapacity,
                    TicketsSold = 0,
                    IsActive = true
                });
            }
            _context.FairDays.AddRange(days);

            // Generate Stalls
            var stalls = new List<Stall>();
            for (int i = 1; i <= model.TotalStalls; i++)
            {
                var category = (StallCategory)(i % 7);
                var size = i % 5 == 0 ? StallSize.PremiumCorner : i % 3 == 0 ? StallSize.Large : i % 2 == 0 ? StallSize.Medium : StallSize.Small;
                var price = size == StallSize.PremiumCorner ? model.BaseStallPrice * 1.5m : size == StallSize.Large ? model.BaseStallPrice * 1.25m : size == StallSize.Medium ? model.BaseStallPrice : model.BaseStallPrice * 0.85m;

                stalls.Add(new Stall
                {
                    FairId = fair.FairId,
                    StallNumber = $"S-{i:D3}",
                    Category = category,
                    Size = size,
                    Price = price,
                    IsBooked = false
                });
            }
            _context.Stalls.AddRange(stalls);
            await _context.SaveChangesAsync();

            return fair.FairId;
        }
    }

    public async Task<IEnumerable<Fair>> GetAllFairsAsync(bool onlyActive = false)
    {
        IQueryable<Fair> query = _context.Fairs
            .Include(f => f.FairDays)
            .Include(f => f.Stalls)
            .OrderByDescending(f => f.StartDate);

        if (onlyActive)
        {
            query = query.Where(f => f.IsActive && f.EndDate >= DateTime.Today);
        }

        return await query.ToListAsync();
    }

    public async Task<Fair?> GetFairByIdAsync(int fairId)
    {
        return await _context.Fairs
            .Include(f => f.FairDays)
            .Include(f => f.Stalls)
            .Include(f => f.JobPostings)
            .FirstOrDefaultAsync(f => f.FairId == fairId);
    }

    public async Task<bool> DeleteFairCascadeAsync(int fairId)
    {
        var fair = await _context.Fairs
            .Include(f => f.FairDays)
            .Include(f => f.Stalls)
            .Include(f => f.StallBookings)
            .Include(f => f.Tickets)
            .Include(f => f.JobPostings)
            .ThenInclude(jp => jp.Applications)
            .FirstOrDefaultAsync(f => f.FairId == fairId);

        if (fair == null) return false;

        _context.Fairs.Remove(fair);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<FairSummaryViewModel>> GetFairSummariesFromViewAsync()
    {
        try
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            const string sql = "SELECT * FROM vw_FairSummary ORDER BY StartDate DESC";
            var summaries = await db.QueryAsync<FairSummaryViewModel>(sql);
            return summaries.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "vw_FairSummary query failed. Executing LINQ summary calculation.");
            var fairs = await _context.Fairs
                .Include(f => f.FairDays)
                .Include(f => f.Stalls)
                .Include(f => f.StallBookings)
                .Include(f => f.Tickets)
                .Include(f => f.JobPostings)
                .ToListAsync();

            return fairs.Select(f =>
            {
                var bookedStalls = f.Stalls.Count(s => s.IsBooked);
                var stallRevenue = f.StallBookings.Where(b => b.PaymentStatus == PaymentStatus.Paid).Sum(b => b.AmountPaid);
                var ticketsSold = f.Tickets.Where(t => t.Status == TicketStatus.Valid).Sum(t => t.Quantity);
                var ticketRevenue = f.Tickets.Where(t => t.Status == TicketStatus.Valid).Sum(t => t.PricePaid);
                var totalCapacity = f.FairDays.Sum(fd => fd.DailyCapacity);

                return new FairSummaryViewModel
                {
                    FairId = f.FairId,
                    Title = f.Title,
                    Location = f.Location,
                    StartDate = f.StartDate,
                    EndDate = f.EndDate,
                    IsActive = f.IsActive,
                    TotalStalls = f.TotalStalls,
                    BookedStalls = bookedStalls,
                    AvailableStalls = f.AvailableStalls,
                    StallOccupancyRate = f.TotalStalls > 0 ? (bookedStalls * 100.0m) / f.TotalStalls : 0,
                    TotalOperatingDays = f.FairDays.Count,
                    TotalTicketCapacity = totalCapacity,
                    TotalTicketsSold = ticketsSold,
                    TicketCapacitySoldPercentage = totalCapacity > 0 ? (ticketsSold * 100.0m) / totalCapacity : 0,
                    StallRevenue = stallRevenue,
                    TicketRevenue = ticketRevenue,
                    TotalRevenue = stallRevenue + ticketRevenue,
                    ActiveJobPostings = f.JobPostings.Count(j => j.IsActive)
                };
            }).ToList();
        }
    }

    public async Task<IEnumerable<DailyVisitorCountViewModel>> GetDailyVisitorCountsFromViewAsync(int? fairId = null)
    {
        try
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM vw_DailyVisitorCount";
            if (fairId.HasValue)
            {
                sql += " WHERE FairId = @FairId";
            }
            sql += " ORDER BY FairDate ASC";

            var dailyCounts = await db.QueryAsync<DailyVisitorCountViewModel>(sql, new { FairId = fairId });
            return dailyCounts.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "vw_DailyVisitorCount query failed. Executing LINQ daily calculation.");
            var query = _context.FairDays.Include(fd => fd.Fair).Include(fd => fd.Tickets).AsQueryable();
            if (fairId.HasValue)
            {
                query = query.Where(fd => fd.FairId == fairId.Value);
            }

            var days = await query.ToListAsync();
            return days.Select(d =>
            {
                var dailyRevenue = d.Tickets.Where(t => t.Status == TicketStatus.Valid).Sum(t => t.PricePaid);
                return new DailyVisitorCountViewModel
                {
                    FairId = d.FairId,
                    FairTitle = d.Fair.Title,
                    FairDate = d.Date,
                    DailyCapacity = d.DailyCapacity,
                    TicketsSold = d.TicketsSold,
                    RemainingCapacity = Math.Max(0, d.DailyCapacity - d.TicketsSold),
                    CapacityUtilizationPercent = d.DailyCapacity > 0 ? (d.TicketsSold * 100.0m) / d.DailyCapacity : 0,
                    DailyTicketRevenue = dailyRevenue
                };
            }).OrderBy(x => x.FairDate).ToList();
        }
    }
}

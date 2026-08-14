using Microsoft.Data.SqlClient;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Services;

/// <summary>
/// Service managing Visitor admissions and atomic ticket purchases
/// </summary>
public class TicketService
{
    private readonly IFairRepository _fairRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        IFairRepository fairRepository,
        ITicketRepository ticketRepository,
        ILogger<TicketService> logger)
    {
        _fairRepository = fairRepository;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<TicketPurchaseViewModel?> GetTicketPurchaseViewDataAsync(int fairId)
    {
        var fair = await _fairRepository.GetFairByIdAsync(fairId);
        if (fair == null) return null;

        var days = await _ticketRepository.GetFairDaysByFairIdAsync(fairId);

        var vm = new TicketPurchaseViewModel
        {
            FairId = fair.FairId,
            FairTitle = fair.Title,
            Location = fair.Location,
            BaseTicketPrice = fair.BaseTicketPrice,
            AvailableDays = days.Select(d => new FairDayOptionDto
            {
                FairDayId = d.FairDayId,
                Date = d.Date,
                DailyCapacity = d.DailyCapacity,
                TicketsSold = d.TicketsSold
            }).ToList()
        };

        return vm;
    }

    public async Task<(bool success, decimal totalPaid, string ticketCode, string message)> BuyTicketsAsync(
        int fairId,
        int fairDayId,
        string visitorId,
        int quantity)
    {
        if (quantity <= 0)
        {
            return (false, 0, string.Empty, "Please enter a valid ticket quantity.");
        }

        try
        {
            var result = await _ticketRepository.BuyTicketViaSpAsync(fairId, fairDayId, visitorId, quantity);
            return (true, result.totalPaid, result.ticketCode, $"Successfully purchased {quantity} ticket(s)! Ticket Code: {result.ticketCode}");
        }
        catch (SqlException ex) when (ex.Number == SqlErrorCodes.DailyCapacityExceeded)
        {
            _logger.LogWarning("Admission capacity reached for FairDay {FairDayId}.", fairDayId);
            return (false, 0, string.Empty, ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error buying tickets for FairDay {FairDayId}.", fairDayId);
            return (false, 0, string.Empty, $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error buying tickets.");
            return (false, 0, string.Empty, "An unexpected error occurred while processing your tickets.");
        }
    }

    public async Task<MyTicketsViewModel> GetVisitorTicketsAsync(string visitorId)
    {
        var tickets = await _ticketRepository.GetVisitorTicketsAsync(visitorId);

        var vm = new MyTicketsViewModel
        {
            Tickets = tickets.Select(t => new VisitorTicketItemDto
            {
                TicketId = t.TicketId,
                TicketCode = t.TicketCode,
                FairTitle = t.Fair?.Title ?? "N/A",
                Location = t.Fair?.Location ?? "N/A",
                VisitDate = t.FairDay?.Date ?? DateTime.MinValue,
                PurchaseDate = t.PurchaseDate,
                PricePaid = t.PricePaid,
                Quantity = t.Quantity,
                Status = t.Status
            }).ToList()
        };

        return vm;
    }
}

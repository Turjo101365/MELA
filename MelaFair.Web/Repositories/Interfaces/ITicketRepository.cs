using MelaFair.Web.Models.Entities;

namespace MelaFair.Web.Repositories.Interfaces;

/// <summary>
/// Repository interface for fair day schedules and admission ticket transactions
/// </summary>
public interface ITicketRepository
{
    Task<(decimal totalPaid, string ticketCode)> BuyTicketViaSpAsync(int fairId, int fairDayId, string visitorId, int quantity);
    Task<IEnumerable<FairDay>> GetFairDaysByFairIdAsync(int fairId);
    Task<IEnumerable<Ticket>> GetVisitorTicketsAsync(string visitorId);
    Task<Ticket?> GetTicketByIdAsync(int ticketId);
}

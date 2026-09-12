using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Services;

namespace MelaFair.Web.Controllers;

/// <summary>
/// Controller for Fair Visitors to browse events, check live capacity, and buy group tickets
/// </summary>
[Authorize(Roles = $"{RoleConstants.Visitor},{RoleConstants.Admin}")]
public class VisitorController : Controller
{
    private readonly FairService _fairService;
    private readonly TicketService _ticketService;

    public VisitorController(FairService fairService, TicketService ticketService)
    {
        _fairService = fairService;
        _ticketService = ticketService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> BrowseFairs()
    {
        var fairs = await _fairService.GetAllFairsAsync(onlyActive: true);
        return View(fairs);
    }

    [HttpGet]
    public async Task<IActionResult> TicketBooking(int fairId)
    {
        var vm = await _ticketService.GetTicketPurchaseViewDataAsync(fairId);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Fair schedule not found.";
            return RedirectToAction("BrowseFairs");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessTicketPurchase(TicketPurchaseViewModel model)
    {
        string? visitorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(visitorId))
        {
            return Challenge();
        }

        if (model.FairDayId <= 0)
        {
            ModelState.AddModelError(nameof(model.FairDayId), "Please select a valid visit date.");
            var refreshedVm = await _ticketService.GetTicketPurchaseViewDataAsync(model.FairId);
            return View("TicketBooking", refreshedVm ?? model);
        }

        if (model.Quantity <= 0 || model.Quantity > 50)
        {
            ModelState.AddModelError(nameof(model.Quantity), "Ticket quantity must be between 1 and 50.");
            var refreshedVm = await _ticketService.GetTicketPurchaseViewDataAsync(model.FairId);
            return View("TicketBooking", refreshedVm ?? model);
        }

        if (string.IsNullOrWhiteSpace(model.PaymentMethod))
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "Please select a payment mode (bKash, Nagad, Rocket, or Visa/Master).");
            var refreshedVm = await _ticketService.GetTicketPurchaseViewDataAsync(model.FairId);
            return View("TicketBooking", refreshedVm ?? model);
        }

        // 1. Payment Gateway Handshake Simulation (realistic network delay & validation)
        await Task.Delay(400);
        bool isPaymentAuthorized = SimulatePaymentGateway(model.PaymentMethod, model.TotalAmount);
        if (!isPaymentAuthorized)
        {
            TempData["ErrorMessage"] = $"Payment authorization failed via {model.PaymentMethod}. Please check your account details and try again.";
            var refreshedVm = await _ticketService.GetTicketPurchaseViewDataAsync(model.FairId);
            return View("TicketBooking", refreshedVm ?? model);
        }

        // 2. Concurrency-Safe Order Persistence via Service / Repository
        // Executes usp_BuyFairTicket with UPDLOCK/ROWLOCK or atomic EF Core fallback, ensuring AvailableCapacity is checked.
        var (success, totalPaid, ticketCode, message) = await _ticketService.BuyTicketsAsync(
            model.FairId,
            model.FairDayId,
            visitorId,
            model.Quantity);

        if (success)
        {
            TempData["SuccessMessage"] = "Payment successful and passes generated!";
            return RedirectToAction("MyTickets");
        }

        TempData["ErrorMessage"] = message;
        var retryVm = await _ticketService.GetTicketPurchaseViewDataAsync(model.FairId);
        return View("TicketBooking", retryVm ?? model);
    }

    /// <summary>
    /// Route alias for form submissions posting to TicketBooking
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> TicketBooking(TicketPurchaseViewModel model) => ProcessTicketPurchase(model);

    private static bool SimulatePaymentGateway(string paymentMethod, decimal amount)
    {
        var supportedMethods = new[] { "bKash", "Nagad", "Rocket", "Visa / Master", "Visa/Master", "Card" };
        return supportedMethods.Any(m => string.Equals(m, paymentMethod, StringComparison.OrdinalIgnoreCase))
               || !string.IsNullOrWhiteSpace(paymentMethod);
    }

    [HttpGet]
    public async Task<IActionResult> MyTickets()
    {
        string? visitorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(visitorId))
        {
            return Challenge();
        }

        var vm = await _ticketService.GetVisitorTicketsAsync(visitorId);
        return View(vm);
    }
}

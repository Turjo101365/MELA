using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Services;

namespace MelaFair.Web.Controllers;

/// <summary>
/// Controller for Fair Vendors to browse available stalls and checkout with high-concurrency safety
/// </summary>
[Authorize(Roles = RoleConstants.Vendor)]
public class VendorController : Controller
{
    private readonly FairService _fairService;
    private readonly StallBookingService _stallBookingService;

    public VendorController(FairService fairService, StallBookingService stallBookingService)
    {
        _fairService = fairService;
        _stallBookingService = stallBookingService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        string? vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(vendorId))
        {
            return Challenge();
        }

        string vendorName = User.Identity?.Name ?? "Valued Vendor";
        string vendorEmail = User.FindFirstValue(ClaimTypes.Email) ?? vendorName;

        var vm = await _stallBookingService.GetVendorDashboardDataAsync(vendorId, vendorName, vendorEmail);
        return View(vm);
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> Marketplace(string? search = null, string? location = null, decimal? maxPrice = null, string? sortOrder = null)
    {
        var activeFairs = (await _fairService.GetAllFairsAsync(onlyActive: true)).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            activeFairs = activeFairs.Where(f => 
                f.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                f.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            activeFairs = activeFairs.Where(f => f.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            activeFairs = activeFairs.Where(f => f.BaseStallPrice <= maxPrice.Value);
        }

        activeFairs = sortOrder switch
        {
            "price_asc" => activeFairs.OrderBy(f => f.BaseStallPrice),
            "price_desc" => activeFairs.OrderByDescending(f => f.BaseStallPrice),
            "stalls_desc" => activeFairs.OrderByDescending(f => f.AvailableStalls),
            "date_asc" => activeFairs.OrderBy(f => f.StartDate),
            _ => activeFairs.OrderBy(f => f.StartDate)
        };

        ViewBag.Search = search;
        ViewBag.Location = location;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.SortOrder = sortOrder;

        return View(activeFairs.ToList());
    }

    [HttpGet]
    public async Task<IActionResult> StallBooking(int fairId)
    {
        var vm = await _stallBookingService.GetStallBookingViewDataAsync(fairId);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Fair not found.";
            return RedirectToAction("Marketplace");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StallBooking(StallBookingViewModel model)
    {
        string? vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(vendorId))
        {
            return Challenge();
        }

        if (model.SelectedStallIds == null || !model.SelectedStallIds.Any())
        {
            ModelState.AddModelError(string.Empty, "Please select at least one stall to proceed with booking.");
            var refreshedVm = await _stallBookingService.GetStallBookingViewDataAsync(model.FairId);
            return View(refreshedVm ?? model);
        }

        var (success, bookedIds, message) = await _stallBookingService.BookStallsAsync(
            model.FairId,
            model.SelectedStallIds,
            vendorId,
            model.Notes);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction("MyStalls");
        }

        TempData["ErrorMessage"] = message;
        var retryVm = await _stallBookingService.GetStallBookingViewDataAsync(model.FairId);
        return View(retryVm ?? model);
    }

    [HttpGet]
    public async Task<IActionResult> MyStalls()
    {
        string? vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(vendorId))
        {
            return Challenge();
        }

        var vm = await _stallBookingService.GetVendorStallsAsync(vendorId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> StallReceipt(int id)
    {
        string? vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(vendorId))
        {
            return Challenge();
        }

        var vm = await _stallBookingService.GetStallReceiptAsync(id, vendorId);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Stall lease certificate was not found or access is unauthorized.";
            return RedirectToAction("MyStalls");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelStall(int bookingId)
    {
        string? vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(vendorId))
        {
            return Challenge();
        }

        var (success, message) = await _stallBookingService.CancelBookingAsync(bookingId, vendorId);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction("MyStalls");
    }
}

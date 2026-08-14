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
    public async Task<IActionResult> Marketplace()
    {
        var activeFairs = await _fairService.GetAllFairsAsync(onlyActive: true);
        return View(activeFairs);
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
}

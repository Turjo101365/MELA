using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Services;

namespace MelaFair.Web.Controllers;

/// <summary>
/// Controller for Fair Administrators to provision fairs, inspect analytics views, and manage events
/// </summary>
[Authorize(Roles = RoleConstants.Admin)]
public class AdminController : Controller
{
    private readonly FairService _fairService;

    public AdminController(FairService fairService)
    {
        _fairService = fairService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var dashboardData = await _fairService.GetAdminDashboardDataAsync();
        return View(dashboardData);
    }

    [HttpGet]
    public IActionResult CreateFair()
    {
        return View(new CreateFairViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFair(CreateFairViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, fairId, message) = await _fairService.CreateFairAsync(model);
        if (success)
        {
            TempData["SuccessMessage"] = $"{message} (Fair #{fairId}: {model.Title})";
            return RedirectToAction("ManageFairs");
        }

        ModelState.AddModelError(string.Empty, message);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ManageFairs()
    {
        var fairs = await _fairService.GetAllFairsAsync();
        return View(fairs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFair(int id)
    {
        bool deleted = await _fairService.DeleteFairAsync(id);
        if (deleted)
        {
            TempData["SuccessMessage"] = "Fair and its associated operating days, stalls, and tickets were successfully deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete fair. Fair not found.";
        }

        return RedirectToAction("ManageFairs");
    }

    [HttpGet]
    public async Task<IActionResult> Analytics(int? fairId = null)
    {
        var summaries = await _fairService.GetFairSummariesAsync();
        var dailyCounts = await _fairService.GetDailyVisitorCountsAsync(fairId);

        ViewBag.SelectedFairId = fairId;
        ViewBag.FairSummaries = summaries;
        ViewBag.AllFairs = await _fairService.GetAllFairsAsync();

        return View(dailyCounts);
    }
}

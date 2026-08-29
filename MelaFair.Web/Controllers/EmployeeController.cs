using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Services;

namespace MelaFair.Web.Controllers;

/// <summary>
/// Controller for Fair Staff & Employees to view job openings and submit employment applications
/// </summary>
[Authorize(Roles = RoleConstants.Employee)]
public class EmployeeController : Controller
{
    private readonly RecruitmentService _recruitmentService;

    public EmployeeController(RecruitmentService recruitmentService)
    {
        _recruitmentService = recruitmentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> JobListings(int? fairId = null)
    {
        var postings = await _recruitmentService.GetActiveJobListingsAsync(fairId);
        return View(postings);
    }

    [HttpGet]
    public async Task<IActionResult> Apply(int jobPostingId)
    {
        var vm = await _recruitmentService.GetJobApplicationViewDataAsync(jobPostingId);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Job posting is closed or was not found.";
            return RedirectToAction("JobListings");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyJobViewModel model)
    {
        string? employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(employeeId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, applicationId, message) = await _recruitmentService.ApplyForJobAsync(model, employeeId);
        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction("MyApplications");
        }

        ModelState.AddModelError(string.Empty, message);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyApplications()
    {
        string? employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(employeeId))
        {
            return Challenge();
        }

        var vm = await _recruitmentService.GetEmployeeApplicationsAsync(employeeId);
        return View(vm);
    }
}

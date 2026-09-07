using Microsoft.AspNetCore.Mvc;
using MelaFair.Web.Services;

namespace MelaFair.Web.Controllers;

/// <summary>
/// Public landing portal highlighting active fairs, statistics, and role entry points
/// </summary>
public class HomeController : Controller
{
    private readonly FairService _fairService;

    public HomeController(FairService fairService)
    {
        _fairService = fairService;
    }

    public async Task<IActionResult> Index()
    {
        var activeFairs = await _fairService.GetAllFairsAsync(onlyActive: true);
        var summaries = await _fairService.GetFairSummariesAsync();

        ViewBag.TotalFairsCount = summaries.Count();
        ViewBag.TotalTicketsSoldCount = summaries.Sum(s => s.TotalTicketsSold);
        ViewBag.TotalStallsBookedCount = summaries.Sum(s => s.BookedStalls);

        return View(activeFairs);
    }

    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(string name, string email, string message)
    {
        TempData["SuccessMessage"] = "Thank you! Your message has been sent successfully. We will get back to you shortly.";
        return RedirectToAction(nameof(Contact));
    }
}


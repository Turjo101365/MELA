using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MelaFair.Web.Data;
namespace MelaFair.Web.Controllers;

public class FairController : Controller
{
    private readonly ApplicationDbContext _context;

    public FairController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Fair/Index
    public async Task<IActionResult> Index()
    {
        
        var fairs = await _context.Fairs
            .Where(f => f.IsActive)
            .ToListAsync();

        return View(fairs);
    }

    // GET: /Fair/Details/5
public async Task<IActionResult> Details(int id)
{
    var fair = await _context.Fairs
        .FirstOrDefaultAsync(f => f.FairId == id);

    if (fair == null)
    {
        return NotFound();
    }

    return View(fair);
}
}
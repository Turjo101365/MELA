using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;

namespace MelaFair.Web.Controllers;

/// <summary>
/// Manages user authentication, registration across 4 roles, and demo session switches
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt. Account not found.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid password or email address.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            UserRole = model.UserRole,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Ensure target role exists and assign
            if (!await _roleManager.RoleExistsAsync(model.UserRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.UserRole));
            }
            await _userManager.AddToRoleAsync(user, model.UserRole);

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = $"Account created successfully! Logged in as {model.UserRole}.";
            return RedirectToLocal(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["InfoMessage"] = "You have been logged out safely.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Quick one-click demo login helper for seamless review and evaluation
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickLogin(string role)
    {
        string email = role.ToLowerInvariant() switch
        {
            "admin" => "admin@mela.com",
            "vendor" => "vendor@mela.com",
            "visitor" => "visitor@mela.com",
            "employee" => "employee@mela.com",
            _ => "visitor@mela.com"
        };

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = $"Logged in as Demo {user.UserRole}: {user.FullName}";

            return role.ToLowerInvariant() switch
            {
                "admin" => RedirectToAction("Dashboard", "Admin"),
                "vendor" => RedirectToAction("Marketplace", "Vendor"),
                "visitor" => RedirectToAction("BrowseFairs", "Visitor"),
                "employee" => RedirectToAction("JobListings", "Employee"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        TempData["ErrorMessage"] = "Demo account not found. Please restart the application to seed default accounts.";
        return RedirectToAction("Login");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (User.IsInRole(RoleConstants.Admin))
            return RedirectToAction("Dashboard", "Admin");
        if (User.IsInRole(RoleConstants.Vendor))
            return RedirectToAction("Marketplace", "Vendor");
        if (User.IsInRole(RoleConstants.Visitor))
            return RedirectToAction("BrowseFairs", "Visitor");
        if (User.IsInRole(RoleConstants.Employee))
            return RedirectToAction("JobListings", "Employee");

        return RedirectToAction("Index", "Home");
    }
}

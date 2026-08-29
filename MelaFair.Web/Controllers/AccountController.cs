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
    public IActionResult Login(string? returnUrl = null, string? email = null, string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel 
        { 
            ReturnUrl = returnUrl, 
            Email = email ?? string.Empty,
            TargetRole = role ?? RoleConstants.Visitor
        });
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
            ModelState.AddModelError(string.Empty, "Invalid login credentials. Account not found with this email.");
            return View(model);
        }

        // Verify password
        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!passwordCheck.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid password. Please check your credentials and try again.");
            return View(model);
        }

        // Verify role if a specific role portal was selected
        if (!string.IsNullOrWhiteSpace(model.TargetRole))
        {
            bool hasRole = await _userManager.IsInRoleAsync(user, model.TargetRole);
            if (!hasRole)
            {
                ModelState.AddModelError(string.Empty, $"Access Denied: This account is registered as '{user.UserRole}', not '{model.TargetRole}'. Please select the '{user.UserRole}' tab to sign in.");
                return View(model);
            }
        }

        // Sign in user
        await _signInManager.SignInAsync(user, model.RememberMe);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = $"Welcome back, {user.FullName}! Logged in as {user.UserRole}.";
        return RedirectToLocal(returnUrl);
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AdminLogin()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(string email, string password)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ErrorMessage = "Please enter valid credentials.";
            return View();
        }

        // Verify it's the admin email
        if (email != "tanmoy.cse.20230104124@aust.edu")
        {
            ViewBag.ErrorMessage = "Invalid admin credentials.";
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ViewBag.ErrorMessage = "Admin account not found.";
            return View();
        }

        // Verify it's an admin user
        if (!await _userManager.IsInRoleAsync(user, RoleConstants.Admin))
        {
            ViewBag.ErrorMessage = "This account is not an administrator account.";
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Welcome Admin! Logged in as {user.FullName}";
            return RedirectToAction("Dashboard", "Admin");
        }

        ViewBag.ErrorMessage = "Invalid password. Access denied.";
        return View();
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

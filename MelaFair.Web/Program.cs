using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MelaFair.Web.Data;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Repositories.Implementations;
using MelaFair.Web.Repositories.Interfaces;
using MelaFair.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Simplified password policy for demo convenience
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie configuration for authentication redirects
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Dependency Injection - Repositories
builder.Services.AddScoped<IFairRepository, FairRepository>();
builder.Services.AddScoped<IStallRepository, StallRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Dependency Injection - Business Services
builder.Services.AddScoped<FairService>();
builder.Services.AddScoped<StallBookingService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<RecruitmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Execute Database Seeder, Schema initialization, and SQL Artifact deployment
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var fairRepo = services.GetRequiredService<IFairRepository>();
    var env = services.GetRequiredService<IWebHostEnvironment>();

    await DbInitializer.InitializeAsync(context, roleManager, userManager, fairRepo, env, logger);
}

app.Run();

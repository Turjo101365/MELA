using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MelaFair.Core.Constants;
using MelaFair.Core.Enums;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Data;

/// <summary>
/// Resilient database initializer that ensures schema creation, runs raw SQL scripts (SPs/Triggers/Views),
/// seeds Identity roles and demo users, and populates sample cultural fair data.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IFairRepository fairRepository,
        IWebHostEnvironment env,
        ILogger logger)
    {
        try
        {
            // 1. Ensure database and core tables exist
            await context.Database.EnsureCreatedAsync();

            // 2. Execute raw SQL scripts (SPs, Triggers, Views)
            await DeploySqlArtifactsAsync(context, env, logger);

            // 3. Seed Identity Roles
            foreach (var roleName in RoleConstants.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 4. Seed Demo Users
            var adminUser = await SeedUserAsync(userManager, "tanmoy.cse.20230104124@aust.edu", "Turjo101365", "System Administrator", RoleConstants.Admin);
            var vendorUser = await SeedUserAsync(userManager, "vendor@mela.com", "Vendor@123456", "Karupanna Crafts Ltd", RoleConstants.Vendor);
            var visitorUser = await SeedUserAsync(userManager, "visitor@mela.com", "Visitor@123456", "Rahim Ahmed", RoleConstants.Visitor);
            var employeeUser = await SeedUserAsync(userManager, "employee@mela.com", "Employee@123456", "Tanvir Hasan", RoleConstants.Employee);

            // 5. Seed Sample Fairs if database has no fairs
            if (!await context.Fairs.AnyAsync())
            {
                logger.LogInformation("Seeding initial cultural fair data...");

                // Fair 1: Dhaka International Boishakhi Mela
                var fair1Model = new CreateFairViewModel
                {
                    Title = "Dhaka International Boishakhi Mela 2026",
                    Description = "The grandest traditional Bengali celebration featuring rural handicrafts, folk music performances, regional culinary delights, and family carnival rides.",
                    Location = "Suhrawardy Udyan, Dhaka",
                    StartDate = DateTime.Today.AddDays(2),
                    EndDate = DateTime.Today.AddDays(9),
                    DailyCapacity = 6000,
                    TotalStalls = 40,
                    BaseStallPrice = 2500,
                    BaseTicketPrice = 80,
                    BannerImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?auto=format&fit=crop&w=1200&q=80"
                };
                int fair1Id = await fairRepository.CreateFairViaSpAsync(fair1Model);

                // Fair 2: Chattogram Folk & Heritage Festival
                var fair2Model = new CreateFairViewModel
                {
                    Title = "Chattogram Folk & Heritage Festival",
                    Description = "Showcasing coastal culture, traditional brass crafts, handloom textiles, and indigenous gourmet delicacies in the heart of the port city.",
                    Location = "CRB Grounds, Chattogram",
                    StartDate = DateTime.Today.AddDays(5),
                    EndDate = DateTime.Today.AddDays(11),
                    DailyCapacity = 4500,
                    TotalStalls = 28,
                    BaseStallPrice = 1800,
                    BaseTicketPrice = 60,
                    BannerImageUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=1200&q=80"
                };
                int fair2Id = await fairRepository.CreateFairViaSpAsync(fair2Model);

                // Fair 3: Sylhet Monsoon Tea & Craft Expo
                var fair3Model = new CreateFairViewModel
                {
                    Title = "Sylhet Monsoon Tea & Craft Expo",
                    Description = "A vibrant festival highlighting premium organic tea gardens, Manipuri handlooms, cane furniture artisans, and live folk fusion acoustics.",
                    Location = "Shahi Eidgah Ground, Sylhet",
                    StartDate = DateTime.Today.AddDays(10),
                    EndDate = DateTime.Today.AddDays(15),
                    DailyCapacity = 3500,
                    TotalStalls = 20,
                    BaseStallPrice = 1500,
                    BaseTicketPrice = 50,
                    BannerImageUrl = "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?auto=format&fit=crop&w=1200&q=80"
                };
                int fair3Id = await fairRepository.CreateFairViaSpAsync(fair3Model);

                // Seed Job Postings for Fair 1 & 2
                var jobPostings = new List<JobPosting>
                {
                    new JobPosting
                    {
                        FairId = fair1Id,
                        Title = "Fair Ground Operations Supervisor",
                        Department = "Operations",
                        Description = "Oversee daily crowd management, stall logistics, and gate security coordination during operating hours.",
                        Requirements = "Minimum 1 year experience in event supervision, prompt communication skills, crisis leadership.",
                        DailyWage = 1200,
                        PositionsAvailable = 4,
                        PositionsFilled = 1,
                        ApplicationDeadline = DateTime.Today.AddDays(15),
                        IsActive = true
                    },
                    new JobPosting
                    {
                        FairId = fair1Id,
                        Title = "Digital Ticketing & Fast-Track Gate Attendant",
                        Department = "Admissions",
                        Description = "Validate visitor electronic tickets and QR passes at primary turnstiles using mobile scanning devices.",
                        Requirements = "Tech savvy, polite customer orientation, ability to operate mobile handheld scanners.",
                        DailyWage = 850,
                        PositionsAvailable = 8,
                        PositionsFilled = 0,
                        ApplicationDeadline = DateTime.Today.AddDays(15),
                        IsActive = true
                    },
                    new JobPosting
                    {
                        FairId = fair2Id,
                        Title = "Cultural Stage & Sound Assistant",
                        Department = "Entertainment",
                        Description = "Assist the stage director with audio-visual equipment setup and artist scheduling during live music shows.",
                        Requirements = "Basic sound engineering knowledge, stamina for evening event execution.",
                        DailyWage = 1000,
                        PositionsAvailable = 3,
                        PositionsFilled = 0,
                        ApplicationDeadline = DateTime.Today.AddDays(18),
                        IsActive = true
                    }
                };

                context.JobPostings.AddRange(jobPostings);
                await context.SaveChangesAsync();

                logger.LogInformation("Sample fairs, stalls, and jobs seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }

    private static async Task<ApplicationUser?> SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null) return existingUser;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            UserRole = role,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        return null;
    }

    private static async Task DeploySqlArtifactsAsync(ApplicationDbContext context, IWebHostEnvironment env, ILogger logger)
    {
        try
        {
            string dbFolderPath = Path.Combine(env.ContentRootPath, "Database");
            if (!Directory.Exists(dbFolderPath)) return;

            // 1. Deploy Views first
            string viewsPath = Path.Combine(dbFolderPath, "Views");
            if (Directory.Exists(viewsPath))
            {
                foreach (var file in Directory.GetFiles(viewsPath, "*.sql"))
                {
                    string script = await File.ReadAllTextAsync(file);
                    await ExecuteBatchSqlAsync(context, script);
                }
            }

            // 2. Deploy Stored Procedures
            string spsPath = Path.Combine(dbFolderPath, "StoredProcedures");
            if (Directory.Exists(spsPath))
            {
                foreach (var file in Directory.GetFiles(spsPath, "*.sql"))
                {
                    string script = await File.ReadAllTextAsync(file);
                    await ExecuteBatchSqlAsync(context, script);
                }
            }

            // 3. Deploy Triggers
            string triggersPath = Path.Combine(dbFolderPath, "Triggers");
            if (Directory.Exists(triggersPath))
            {
                foreach (var file in Directory.GetFiles(triggersPath, "*.sql"))
                {
                    string script = await File.ReadAllTextAsync(file);
                    await ExecuteBatchSqlAsync(context, script);
                }
            }

            logger.LogInformation("All SQL Stored Procedures, Triggers, and Views successfully deployed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Note: SQL script artifacts deployment skipped or partially applied.");
        }
    }

    private static async Task ExecuteBatchSqlAsync(ApplicationDbContext context, string sqlScript)
    {
        var batches = System.Text.RegularExpressions.Regex.Split(
            sqlScript,
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        foreach (var batch in batches)
        {
            if (!string.IsNullOrWhiteSpace(batch))
            {
                await context.Database.ExecuteSqlRawAsync(batch);
            }
        }
    }
}

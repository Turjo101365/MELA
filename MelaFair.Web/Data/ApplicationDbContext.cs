using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;

namespace MelaFair.Web.Data;

/// <summary>
/// Entity Framework Core Database Context managing Identity and cultural fair domain tables
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Fair> Fairs => Set<Fair>();
    public DbSet<FairDay> FairDays => Set<FairDay>();
    public DbSet<Stall> Stalls => Set<Stall>();
    public DbSet<StallBooking> StallBookings => Set<StallBooking>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    // Keyless View Mappings
    public DbSet<FairSummaryViewModel> FairSummaries => Set<FairSummaryViewModel>();
    public DbSet<DailyVisitorCountViewModel> DailyVisitorCounts => Set<DailyVisitorCountViewModel>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Fair
        builder.Entity<Fair>(entity =>
        {
            entity.ToTable(tb => tb.UseSqlOutputClause(false));
            entity.HasKey(e => e.FairId);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BaseStallPrice).HasPrecision(18, 2);
            entity.Property(e => e.BaseTicketPrice).HasPrecision(18, 2);
            entity.HasIndex(e => e.IsActive);
        });

        // Configure FairDay with Cascade Delete
        builder.Entity<FairDay>(entity =>
        {
            entity.ToTable(tb => tb.UseSqlOutputClause(false));
            entity.HasKey(e => e.FairDayId);
            entity.HasOne(e => e.Fair)
                  .WithMany(f => f.FairDays)
                  .HasForeignKey(e => e.FairId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.FairId, e.Date }).IsUnique();
        });

        // Configure Stall with Cascade Delete
        builder.Entity<Stall>(entity =>
        {
            entity.ToTable(tb => tb.UseSqlOutputClause(false));
            entity.HasKey(e => e.StallId);
            entity.Property(e => e.StallNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);

            entity.HasOne(e => e.Fair)
                  .WithMany(f => f.Stalls)
                  .HasForeignKey(e => e.FairId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.FairId, e.StallNumber }).IsUnique();
        });

        // Configure StallBooking
        builder.Entity<StallBooking>(entity =>
        {
            entity.ToTable(tb => tb.UseSqlOutputClause(false));
            entity.HasKey(e => e.StallBookingId);
            entity.Property(e => e.AmountPaid).HasPrecision(18, 2);
            entity.Property(e => e.TransactionReference).HasMaxLength(100);

            entity.HasOne(e => e.Stall)
                  .WithMany(s => s.Bookings)
                  .HasForeignKey(e => e.StallId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Fair)
                  .WithMany(f => f.StallBookings)
                  .HasForeignKey(e => e.FairId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Vendor)
                  .WithMany(u => u.StallBookings)
                  .HasForeignKey(e => e.VendorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Ticket
        builder.Entity<Ticket>(entity =>
        {
            entity.ToTable(tb => tb.UseSqlOutputClause(false));
            entity.HasKey(e => e.TicketId);
            entity.Property(e => e.TicketCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PricePaid).HasPrecision(18, 2);

            entity.HasOne(e => e.Fair)
                  .WithMany(f => f.Tickets)
                  .HasForeignKey(e => e.FairId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FairDay)
                  .WithMany(fd => fd.Tickets)
                  .HasForeignKey(e => e.FairDayId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Visitor)
                  .WithMany(u => u.Tickets)
                  .HasForeignKey(e => e.VisitorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TicketCode).IsUnique();
        });

        // Configure JobPosting
        builder.Entity<JobPosting>(entity =>
        {
            entity.ToTable(tb => tb.UseSqlOutputClause(false));
            entity.HasKey(e => e.JobPostingId);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DailyWage).HasPrecision(18, 2);

            entity.HasOne(e => e.Fair)
                  .WithMany(f => f.JobPostings)
                  .HasForeignKey(e => e.FairId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure JobApplication
        builder.Entity<JobApplication>(entity =>
        {
            entity.ToTable(tb => tb.UseSqlOutputClause(false));
            entity.HasKey(e => e.JobApplicationId);

            entity.HasOne(e => e.JobPosting)
                  .WithMany(jp => jp.Applications)
                  .HasForeignKey(e => e.JobPostingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                  .WithMany(u => u.JobApplications)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate applications per employee per job
            entity.HasIndex(e => new { e.JobPostingId, e.EmployeeId }).IsUnique();
        });

        // Configure Keyless Database Views
        builder.Entity<FairSummaryViewModel>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_FairSummary");
            e.Property(p => p.StallRevenue).HasPrecision(18, 2);
            e.Property(p => p.TicketRevenue).HasPrecision(18, 2);
            e.Property(p => p.TotalRevenue).HasPrecision(18, 2);
            e.Property(p => p.StallOccupancyRate).HasPrecision(18, 2);
            e.Property(p => p.TicketCapacitySoldPercentage).HasPrecision(18, 2);
        });

        builder.Entity<DailyVisitorCountViewModel>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_DailyVisitorCount");
            e.Property(p => p.CapacityUtilizationPercent).HasPrecision(18, 2);
            e.Property(p => p.DailyTicketRevenue).HasPrecision(18, 2);
        });
    }
}

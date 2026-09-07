using Microsoft.Data.SqlClient;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Repositories.Interfaces;
using MelaFair.Core.Enums;

namespace MelaFair.Web.Services;

/// <summary>
/// Service managing Employee job listings and application submissions
/// </summary>
public class RecruitmentService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<RecruitmentService> _logger;
    private readonly IStallRepository _stallRepository;
    private readonly IFairRepository _fairRepository;

    public RecruitmentService(
        IEmployeeRepository employeeRepository,
        ILogger<RecruitmentService> logger,
        IStallRepository stallRepository,
        IFairRepository fairRepository)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
        _stallRepository = stallRepository;
        _fairRepository = fairRepository;
    }

    public async Task<IEnumerable<JobPosting>> GetActiveJobListingsAsync(int? fairId = null)
    {
        return await _employeeRepository.GetActiveJobPostingsAsync(fairId);
    }

    public async Task<ApplyJobViewModel?> GetJobApplicationViewDataAsync(int jobPostingId)
    {
        var job = await _employeeRepository.GetJobPostingByIdAsync(jobPostingId);
        if (job == null || !job.IsActive || job.ApplicationDeadline.Date < DateTime.Today || job.PositionsFilled >= job.PositionsAvailable) return null;

        var vm = new ApplyJobViewModel
        {
            JobPostingId = job.JobPostingId,
            JobTitle = job.Title,
            FairTitle = job.Fair?.Title ?? "Mela Fair",
            Department = job.Department,
            DailyWage = job.DailyWage,
            Requirements = job.Requirements
        };

        return vm;
    }

    public async Task<(bool success, int applicationId, string message)> ApplyForJobAsync(
        ApplyJobViewModel model, 
        string employeeId)
    {
        try
        {
            var job = await _employeeRepository.GetJobPostingByIdAsync(model.JobPostingId);
            if (job == null || !job.IsActive || job.ApplicationDeadline.Date < DateTime.Today || job.PositionsFilled >= job.PositionsAvailable)
                return (false, 0, "This job posting is no longer accepting applications.");
            int applicationId = await _employeeRepository.ApplyJobViaSpAsync(
                model.JobPostingId, 
                employeeId, 
                model.ResumeSummary, 
                model.ExperienceYears, 
                model.ContactPhone);

            return (true, applicationId, "Your application has been submitted successfully!");
        }
        catch (SqlException ex) when (ex.Number == SqlErrorCodes.DuplicateJobApplication)
        {
            return (false, 0, "You have already submitted an application for this position.");
        }
        catch (SqlException ex) when (ex.Number == SqlErrorCodes.JobPositionsFilled)
        {
            return (false, 0, "All available openings for this role have already been filled.");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error applying for job {JobPostingId}.", model.JobPostingId);
            return (false, 0, $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error applying for job.");
            return (false, 0, "An unexpected error occurred while submitting your application.");
        }
    }

    public async Task<MyApplicationsViewModel> GetEmployeeApplicationsAsync(string employeeId)
    {
        var applications = await _employeeRepository.GetEmployeeApplicationsAsync(employeeId);

        var vm = new MyApplicationsViewModel
        {
            Applications = applications.Select(a => new JobApplicationItemDto
            {
                ApplicationId = a.JobApplicationId,
                JobTitle = a.JobPosting?.Title ?? "N/A",
                FairTitle = a.JobPosting?.Fair?.Title ?? "N/A",
                Department = a.JobPosting?.Department ?? "N/A",
                DailyWage = a.JobPosting?.DailyWage ?? 0,
                ApplicationDate = a.ApplicationDate,
                Status = a.Status,
                ResumeSummary = a.ResumeSummary
            }).ToList()
        };

        return vm;
    }

    public async Task<List<VendorJobPostingItemDto>> GetVendorJobsAsync(string vendorId)
    {
        var jobs = await _employeeRepository.GetVendorJobPostingsAsync(vendorId);
        return jobs.Select(j => new VendorJobPostingItemDto
        {
            JobPostingId = j.JobPostingId, Title = j.Title, FairTitle = j.Fair.Title,
            PositionsAvailable = j.PositionsAvailable, PositionsFilled = j.PositionsFilled,
            ApplicationCount = j.Applications.Count, IsActive = j.IsActive,
            ApplicationDeadline = j.ApplicationDeadline
        }).ToList();
    }

    public async Task<VendorJobPostingViewModel?> GetVendorJobFormAsync(string vendorId, int? jobPostingId = null)
    {
        var fairIds = (await _stallRepository.GetVendorFairIdsAsync(vendorId)).ToHashSet();
        var fairs = (await _fairRepository.GetAllFairsAsync(true)).Where(f => fairIds.Contains(f.FairId));
        if (jobPostingId is null)
            return new VendorJobPostingViewModel { AvailableFairs = fairs.Select(f => new FairOptionDto { FairId = f.FairId, Title = f.Title }).ToList() };

        var job = await _employeeRepository.GetVendorJobPostingAsync(jobPostingId.Value, vendorId);
        if (job is null) return null;
        return new VendorJobPostingViewModel
        {
            JobPostingId = job.JobPostingId, FairId = job.FairId, Title = job.Title, Department = job.Department,
            Description = job.Description, Requirements = job.Requirements, DailyWage = job.DailyWage,
            PositionsAvailable = job.PositionsAvailable, ApplicationDeadline = job.ApplicationDeadline,
            IsActive = job.IsActive, AvailableFairs = fairs.Select(f => new FairOptionDto { FairId = f.FairId, Title = f.Title }).ToList()
        };
    }

    public async Task<(bool success, string message)> SaveVendorJobAsync(VendorJobPostingViewModel model, string vendorId)
    {
        var allowedFairIds = (await _stallRepository.GetVendorFairIdsAsync(vendorId)).ToHashSet();
        if (!allowedFairIds.Contains(model.FairId)) return (false, "You may only post jobs for fairs where you lease a stall.");
        if (model.ApplicationDeadline.Date < DateTime.Today) return (false, "The application deadline cannot be in the past.");

        JobPosting job;
        var isNew = model.JobPostingId == 0;
        if (model.JobPostingId == 0)
        {
            job = new JobPosting { VendorId = vendorId, CreatedAt = DateTime.UtcNow };
        }
        else
        {
            job = await _employeeRepository.GetVendorJobPostingAsync(model.JobPostingId, vendorId)
                ?? throw new InvalidOperationException("Job posting not found.");
            if (model.PositionsAvailable < job.PositionsFilled) return (false, "Openings cannot be less than accepted employees.");
        }
        job.FairId = model.FairId; job.Title = model.Title; job.Department = model.Department;
        job.Description = model.Description; job.Requirements = model.Requirements; job.DailyWage = model.DailyWage;
        job.PositionsAvailable = model.PositionsAvailable; job.ApplicationDeadline = model.ApplicationDeadline.Date;
        job.IsActive = model.IsActive;
        if (isNew) await _employeeRepository.CreateJobPostingAsync(job);
        else await _employeeRepository.SaveChangesAsync();
        return (true, isNew ? "Job posting created." : "Job posting updated.");
    }

    public async Task<bool> DeleteVendorJobAsync(int jobPostingId, string vendorId)
    {
        var job = await _employeeRepository.GetVendorJobPostingAsync(jobPostingId, vendorId);
        if (job is null) return false;
        job.IsActive = false; // Retain applications for an auditable employment record.
        await _employeeRepository.SaveChangesAsync();
        return true;
    }

    public async Task<VendorJobApplicationsViewModel?> GetVendorApplicationsAsync(int jobPostingId, string vendorId)
    {
        var job = await _employeeRepository.GetVendorJobPostingAsync(jobPostingId, vendorId);
        if (job is null) return null;
        var applications = await _employeeRepository.GetVendorApplicationsAsync(jobPostingId, vendorId);
        return new VendorJobApplicationsViewModel { JobPostingId = jobPostingId, JobTitle = job.Title,
            Applications = applications.Select(a => new VendorApplicationItemDto { ApplicationId = a.JobApplicationId,
                EmployeeName = a.Employee.FullName, EmployeeEmail = a.Employee.Email ?? string.Empty, ContactPhone = a.ContactPhone,
                ResumeSummary = a.ResumeSummary, ExperienceYears = a.ExperienceYears, ApplicationDate = a.ApplicationDate, Status = a.Status }).ToList() };
    }

    public async Task<(bool success, string message)> ReviewVendorApplicationAsync(int applicationId, ApplicationStatus status, string vendorId)
    {
        if (status is not (ApplicationStatus.Accepted or ApplicationStatus.Rejected)) return (false, "Choose Accepted or Rejected.");
        var jobs = await _employeeRepository.GetVendorJobPostingsAsync(vendorId);
        var application = jobs.SelectMany(j => j.Applications).FirstOrDefault(a => a.JobApplicationId == applicationId);
        if (application is null) return (false, "Application not found.");
        if (application.Status != ApplicationStatus.Pending) return (false, "Only pending applications can be reviewed.");
        var job = application.JobPosting;
        if (status == ApplicationStatus.Accepted && job.PositionsFilled >= job.PositionsAvailable) return (false, "All positions have already been filled.");
        application.Status = status;
        if (status == ApplicationStatus.Accepted) job.PositionsFilled++;
        await _employeeRepository.SaveChangesAsync();
        return (true, $"Application {status.ToString().ToLowerInvariant()}.");
    }
}

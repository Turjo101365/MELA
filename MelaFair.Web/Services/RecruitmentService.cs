using Microsoft.Data.SqlClient;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Services;

/// <summary>
/// Service managing Employee job listings and application submissions
/// </summary>
public class RecruitmentService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<RecruitmentService> _logger;

    public RecruitmentService(
        IEmployeeRepository employeeRepository,
        ILogger<RecruitmentService> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<JobPosting>> GetActiveJobListingsAsync(int? fairId = null)
    {
        return await _employeeRepository.GetActiveJobPostingsAsync(fairId);
    }

    public async Task<ApplyJobViewModel?> GetJobApplicationViewDataAsync(int jobPostingId)
    {
        var job = await _employeeRepository.GetJobPostingByIdAsync(jobPostingId);
        if (job == null || !job.IsActive) return null;

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
}

using MelaFair.Web.Models.Entities;

namespace MelaFair.Web.Repositories.Interfaces;

/// <summary>
/// Repository interface for job postings and employee recruitment applications
/// </summary>
public interface IEmployeeRepository
{
    Task<IEnumerable<JobPosting>> GetActiveJobPostingsAsync(int? fairId = null);
    Task<JobPosting?> GetJobPostingByIdAsync(int jobPostingId);
    Task<int> ApplyJobViaSpAsync(int jobPostingId, string employeeId, string resumeSummary, int experienceYears, string contactPhone);
    Task<IEnumerable<JobApplication>> GetEmployeeApplicationsAsync(string employeeId);
    Task<int> CreateJobPostingAsync(JobPosting posting);
}

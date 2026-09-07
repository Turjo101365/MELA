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
    Task<IEnumerable<JobPosting>> GetVendorJobPostingsAsync(string vendorId, bool isAdmin = false);
    Task<JobPosting?> GetVendorJobPostingAsync(int jobPostingId, string vendorId, bool isAdmin = false);
    Task<IEnumerable<JobApplication>> GetVendorApplicationsAsync(int jobPostingId, string vendorId, bool isAdmin = false);
    Task SaveChangesAsync();
}

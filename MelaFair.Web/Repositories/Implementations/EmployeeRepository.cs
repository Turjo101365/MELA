using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MelaFair.Core.Constants;
using MelaFair.Core.Enums;
using MelaFair.Web.Data;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Repositories.Implementations;

/// <summary>
/// Employee repository executing usp_RecruitEmployee and managing job opportunities with resilient fallback
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;
    private readonly ILogger<EmployeeRepository> _logger;

    public EmployeeRepository(ApplicationDbContext context, IConfiguration configuration, ILogger<EmployeeRepository> logger)
    {
        _context = context;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public async Task<IEnumerable<JobPosting>> GetActiveJobPostingsAsync(int? fairId = null)
    {
        IQueryable<JobPosting> query = _context.JobPostings
            .Include(j => j.Fair)
            .Where(j => j.IsActive && j.PositionsFilled < j.PositionsAvailable && j.ApplicationDeadline >= DateTime.Today);

        if (fairId.HasValue)
        {
            query = query.Where(j => j.FairId == fairId.Value);
        }

        return await query.OrderByDescending(j => j.CreatedAt).ToListAsync();
    }

    public async Task<JobPosting?> GetJobPostingByIdAsync(int jobPostingId)
    {
        return await _context.JobPostings
            .Include(j => j.Fair)
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.JobPostingId == jobPostingId);
    }

    public async Task<int> ApplyJobViaSpAsync(int jobPostingId, string employeeId, string resumeSummary, int experienceYears, string contactPhone)
    {
        try
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@JobPostingId", jobPostingId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@EmployeeId", employeeId, DbType.String, ParameterDirection.Input, 450);
            parameters.Add("@ResumeSummary", resumeSummary, DbType.String, ParameterDirection.Input, 1000);
            parameters.Add("@ExperienceYears", experienceYears, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@ContactPhone", contactPhone, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@ApplicationId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await db.ExecuteAsync("usp_RecruitEmployee", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@ApplicationId");
        }
        catch (SqlException ex) when (ex.Number == SqlErrorCodes.DuplicateJobApplication || ex.Number == SqlErrorCodes.JobPositionsFilled)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stored procedure usp_RecruitEmployee failed. Executing atomic EF Core fallback.");

            var exists = await _context.JobApplications.AnyAsync(a => a.JobPostingId == jobPostingId && a.EmployeeId == employeeId);
            if (exists)
            {
                throw new InvalidOperationException("You have already submitted an application for this position.");
            }

            var application = new JobApplication
            {
                JobPostingId = jobPostingId,
                EmployeeId = employeeId,
                ApplicationDate = DateTime.UtcNow,
                Status = ApplicationStatus.Pending,
                ResumeSummary = resumeSummary,
                ExperienceYears = experienceYears,
                ContactPhone = contactPhone
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            return application.JobApplicationId;
        }
    }

    public async Task<IEnumerable<JobApplication>> GetEmployeeApplicationsAsync(string employeeId)
    {
        return await _context.JobApplications
            .Include(a => a.JobPosting)
            .ThenInclude(jp => jp.Fair)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();
    }

    public async Task<int> CreateJobPostingAsync(JobPosting posting)
    {
        _context.JobPostings.Add(posting);
        await _context.SaveChangesAsync();
        return posting.JobPostingId;
    }
}

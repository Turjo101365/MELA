using Microsoft.Data.SqlClient;
using MelaFair.Core.Constants;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;
using MelaFair.Web.Repositories.Interfaces;

namespace MelaFair.Web.Services;

/// <summary>
/// Service coordinating Fair management, analytics, and admin dashboard operations
/// </summary>
public class FairService
{
    private readonly IFairRepository _fairRepository;
    private readonly ILogger<FairService> _logger;

    public FairService(IFairRepository fairRepository, ILogger<FairService> logger)
    {
        _fairRepository = fairRepository;
        _logger = logger;
    }

    public async Task<(bool success, int fairId, string message)> CreateFairAsync(CreateFairViewModel model)
    {
        if (model.EndDate < model.StartDate)
        {
            return (false, 0, "Fair end date must be greater than or equal to start date.");
        }

        try
        {
            int fairId = await _fairRepository.CreateFairViaSpAsync(model);
            return (true, fairId, "Fair and operating days created successfully!");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error creating fair.");
            return (false, 0, $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating fair.");
            return (false, 0, $"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<IEnumerable<Fair>> GetAllFairsAsync(bool onlyActive = false)
    {
        return await _fairRepository.GetAllFairsAsync(onlyActive);
    }

    public async Task<Fair?> GetFairByIdAsync(int fairId)
    {
        return await _fairRepository.GetFairByIdAsync(fairId);
    }

    public async Task<bool> DeleteFairAsync(int fairId)
    {
        return await _fairRepository.DeleteFairCascadeAsync(fairId);
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardDataAsync()
    {
        var summaries = (await _fairRepository.GetFairSummariesFromViewAsync()).ToList();
        var dailyAnalytics = (await _fairRepository.GetDailyVisitorCountsFromViewAsync()).ToList();

        var vm = new AdminDashboardViewModel
        {
            TotalFairs = summaries.Count,
            ActiveFairs = summaries.Count(f => f.IsActive && f.EndDate >= DateTime.Today),
            TotalStalls = summaries.Sum(f => f.TotalStalls),
            TotalStallsBooked = summaries.Sum(f => f.BookedStalls),
            TotalTicketsSold = summaries.Sum(f => f.TotalTicketsSold),
            TotalStallRevenue = summaries.Sum(f => f.StallRevenue),
            TotalTicketRevenue = summaries.Sum(f => f.TicketRevenue),
            TotalJobApplications = summaries.Sum(f => f.ActiveJobPostings),
            FairSummaries = summaries,
            DailyVisitorAnalytics = dailyAnalytics
        };

        return vm;
    }

    public async Task<IEnumerable<FairSummaryViewModel>> GetFairSummariesAsync()
    {
        return await _fairRepository.GetFairSummariesFromViewAsync();
    }

    public async Task<IEnumerable<DailyVisitorCountViewModel>> GetDailyVisitorCountsAsync(int? fairId = null)
    {
        return await _fairRepository.GetDailyVisitorCountsFromViewAsync(fairId);
    }
}

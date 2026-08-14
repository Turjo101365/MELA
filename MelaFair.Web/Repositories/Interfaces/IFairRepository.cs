using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;

namespace MelaFair.Web.Repositories.Interfaces;

/// <summary>
/// Repository interface for Fair entity operations, stored procedures, and analytics views
/// </summary>
public interface IFairRepository
{
    Task<int> CreateFairViaSpAsync(CreateFairViewModel model);
    Task<IEnumerable<Fair>> GetAllFairsAsync(bool onlyActive = false);
    Task<Fair?> GetFairByIdAsync(int fairId);
    Task<bool> DeleteFairCascadeAsync(int fairId);
    Task<IEnumerable<FairSummaryViewModel>> GetFairSummariesFromViewAsync();
    Task<IEnumerable<DailyVisitorCountViewModel>> GetDailyVisitorCountsFromViewAsync(int? fairId = null);
}

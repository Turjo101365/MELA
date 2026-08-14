using MelaFair.Core.Constants;
using MelaFair.Core.Enums;
using MelaFair.Web.Models.Entities;
using MelaFair.Web.Models.ViewModels;

namespace MelaFair.Tests;

/// <summary>
/// Domain model and business logic unit tests
/// </summary>
public class MelaSystemTests
{
    [Fact]
    public void RoleConstants_ShouldContainAllFourRoles()
    {
        Assert.Equal(4, RoleConstants.AllRoles.Length);
        Assert.Contains(RoleConstants.Admin, RoleConstants.AllRoles);
        Assert.Contains(RoleConstants.Vendor, RoleConstants.AllRoles);
        Assert.Contains(RoleConstants.Visitor, RoleConstants.AllRoles);
        Assert.Contains(RoleConstants.Employee, RoleConstants.AllRoles);
    }

    [Fact]
    public void FairDayOptionDto_CalculatesRemainingCapacityCorrectly()
    {
        var day = new FairDayOptionDto
        {
            DailyCapacity = 5000,
            TicketsSold = 3200
        };

        Assert.Equal(1800, day.RemainingCapacity);
        Assert.False(day.IsSoldOut);
    }

    [Fact]
    public void FairDayOptionDto_MarksSoldOutWhenCapacityReached()
    {
        var day = new FairDayOptionDto
        {
            DailyCapacity = 1000,
            TicketsSold = 1000
        };

        Assert.Equal(0, day.RemainingCapacity);
        Assert.True(day.IsSoldOut);
    }

    [Fact]
    public void FairSummaryViewModel_TotalRevenueCalculatesCorrectly()
    {
        var summary = new FairSummaryViewModel
        {
            StallRevenue = 50000,
            TicketRevenue = 25000,
            TotalRevenue = 75000
        };

        Assert.Equal(75000, summary.TotalRevenue);
    }
}

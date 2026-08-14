-- =============================================
-- View: vw_FairSummary
-- Description: Aggregated executive summary of cultural fairs including occupancy, ticket sales, and total revenue
-- =============================================
CREATE OR ALTER VIEW vw_FairSummary
AS
SELECT 
    f.FairId,
    f.Title,
    f.Location,
    f.StartDate,
    f.EndDate,
    f.IsActive,
    f.TotalStalls,
    ISNULL(s_stats.BookedStalls, 0) AS BookedStalls,
    f.AvailableStalls,
    CASE 
        WHEN f.TotalStalls > 0 
        THEN CAST((ISNULL(s_stats.BookedStalls, 0) * 100.0) / f.TotalStalls AS DECIMAL(18, 2))
        ELSE 0 
    END AS StallOccupancyRate,
    ISNULL(fd_stats.TotalOperatingDays, 0) AS TotalOperatingDays,
    ISNULL(fd_stats.TotalTicketCapacity, 0) AS TotalTicketCapacity,
    ISNULL(t_stats.TotalTicketsSold, 0) AS TotalTicketsSold,
    CASE 
        WHEN ISNULL(fd_stats.TotalTicketCapacity, 0) > 0 
        THEN CAST((ISNULL(t_stats.TotalTicketsSold, 0) * 100.0) / fd_stats.TotalTicketCapacity AS DECIMAL(18, 2))
        ELSE 0 
    END AS TicketCapacitySoldPercentage,
    ISNULL(s_stats.StallRevenue, 0) AS StallRevenue,
    ISNULL(t_stats.TicketRevenue, 0) AS TicketRevenue,
    (ISNULL(s_stats.StallRevenue, 0) + ISNULL(t_stats.TicketRevenue, 0)) AS TotalRevenue,
    ISNULL(j_stats.ActiveJobPostings, 0) AS ActiveJobPostings
FROM Fairs f
OUTER APPLY (
    SELECT 
        COUNT(CASE WHEN IsBooked = 1 THEN 1 END) AS BookedStalls,
        SUM(CASE WHEN sb.PaymentStatus = 1 THEN sb.AmountPaid ELSE 0 END) AS StallRevenue
    FROM Stalls s
    LEFT JOIN StallBookings sb ON s.StallId = sb.StallId
    WHERE s.FairId = f.FairId
) s_stats
OUTER APPLY (
    SELECT 
        COUNT(*) AS TotalOperatingDays,
        SUM(DailyCapacity) AS TotalTicketCapacity
    FROM FairDays
    WHERE FairId = f.FairId
) fd_stats
OUTER APPLY (
    SELECT 
        SUM(Quantity) AS TotalTicketsSold,
        SUM(PricePaid) AS TicketRevenue
    FROM Tickets
    WHERE FairId = f.FairId AND [Status] = 0
) t_stats
OUTER APPLY (
    SELECT 
        COUNT(*) AS ActiveJobPostings
    FROM JobPostings
    WHERE FairId = f.FairId AND IsActive = 1
) j_stats;

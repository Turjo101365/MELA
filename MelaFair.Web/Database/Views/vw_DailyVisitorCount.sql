-- =============================================
-- View: vw_DailyVisitorCount
-- Description: Day-by-day attendance capacity and ticket revenue analytics per fair
-- =============================================
CREATE OR ALTER VIEW vw_DailyVisitorCount
AS
SELECT 
    fd.FairDayId,
    f.FairId,
    f.Title AS FairTitle,
    fd.[Date] AS FairDate,
    fd.DailyCapacity,
    fd.TicketsSold,
    (fd.DailyCapacity - fd.TicketsSold) AS RemainingCapacity,
    CASE 
        WHEN fd.DailyCapacity > 0 
        THEN CAST((fd.TicketsSold * 100.0) / fd.DailyCapacity AS DECIMAL(18, 2))
        ELSE 0 
    END AS CapacityUtilizationPercent,
    ISNULL(t_daily.DailyTicketRevenue, 0) AS DailyTicketRevenue
FROM FairDays fd
JOIN Fairs f ON fd.FairId = f.FairId
OUTER APPLY (
    SELECT SUM(PricePaid) AS DailyTicketRevenue
    FROM Tickets
    WHERE FairDayId = fd.FairDayId AND [Status] = 0
) t_daily;

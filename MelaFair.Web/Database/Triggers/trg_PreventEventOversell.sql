-- =============================================
-- Trigger: trg_PreventEventOversell
-- Description: Safety net trigger preventing any ticket insertion from exceeding FairDay capacity
-- =============================================
CREATE OR ALTER TRIGGER trg_PreventEventOversell
ON Tickets
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if any FairDay capacity has been breached
    IF EXISTS (
        SELECT 1
        FROM FairDays fd
        JOIN (
            SELECT FairDayId, SUM(Quantity) AS TotalSold
            FROM Tickets
            WHERE [Status] = 0 -- Valid tickets
            GROUP BY FairDayId
        ) t ON fd.FairDayId = t.FairDayId
        WHERE t.TotalSold > fd.DailyCapacity
    )
    BEGIN
        RAISERROR ('Trigger Safety Error: Ticket sales exceed maximum daily admission quota for the fair day.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
END;

-- =============================================
-- Trigger: trg_StallSoldCountUpdate
-- Description: Maintains Fair.AvailableStalls cache accurately whenever stalls are updated or inserted
-- =============================================
CREATE OR ALTER TRIGGER trg_StallSoldCountUpdate
ON Stalls
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Update available stall counts for affected fairs
    WITH AffectedFairs AS (
        SELECT FairId FROM inserted
        UNION
        SELECT FairId FROM deleted
    )
    UPDATE f
    SET AvailableStalls = (
        SELECT COUNT(*)
        FROM Stalls s
        WHERE s.FairId = f.FairId AND s.IsBooked = 0
    )
    FROM Fairs f
    JOIN AffectedFairs af ON f.FairId = af.FairId;
END;

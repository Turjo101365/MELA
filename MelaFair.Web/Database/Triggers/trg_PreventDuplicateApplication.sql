-- =============================================
-- Trigger: trg_PreventDuplicateApplication
-- Description: Safety net trigger preventing multiple job applications from the same employee for one job
-- =============================================
CREATE OR ALTER TRIGGER trg_PreventDuplicateApplication
ON JobApplications
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM JobApplications ja
        JOIN inserted i ON ja.JobPostingId = i.JobPostingId AND ja.EmployeeId = i.EmployeeId
        WHERE ja.JobApplicationId <> i.JobApplicationId
    )
    BEGIN
        RAISERROR ('Trigger Safety Error: Multiple applications from the same applicant for one job posting are forbidden.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
END;

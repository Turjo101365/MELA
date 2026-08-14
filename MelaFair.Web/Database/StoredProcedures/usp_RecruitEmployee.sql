-- =============================================
-- Stored Procedure: usp_RecruitEmployee
-- Description: Concurrency-safe job application submission with duplicate prevention and capacity checks
-- =============================================
CREATE OR ALTER PROCEDURE usp_RecruitEmployee
    @JobPostingId INT,
    @EmployeeId NVARCHAR(450),
    @ResumeSummary NVARCHAR(1000),
    @ExperienceYears INT,
    @ContactPhone NVARCHAR(50),
    @ApplicationId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Row-level lock on JobPosting
        DECLARE @PositionsAvailable INT;
        DECLARE @PositionsFilled INT;
        DECLARE @IsActive BIT;

        SELECT 
            @PositionsAvailable = PositionsAvailable,
            @PositionsFilled = PositionsFilled,
            @IsActive = IsActive
        FROM JobPostings WITH (UPDLOCK, ROWLOCK)
        WHERE JobPostingId = @JobPostingId;

        IF @PositionsAvailable IS NULL OR @IsActive = 0
        BEGIN
            THROW 50004, 'The requested job posting is no longer active or was not found.', 1;
        END;

        -- 2. Check if positions are already filled
        IF @PositionsFilled >= @PositionsAvailable
        BEGIN
            THROW 50005, 'All open positions for this role have already been filled.', 1;
        END;

        -- 3. Check for existing application from same employee
        IF EXISTS (
            SELECT 1 
            FROM JobApplications WITH (HOLDLOCK)
            WHERE JobPostingId = @JobPostingId AND EmployeeId = @EmployeeId
        )
        BEGIN
            THROW 50003, 'You have already submitted an application for this job posting.', 1;
        END;

        -- 4. Insert JobApplication record
        INSERT INTO JobApplications (
            JobPostingId, EmployeeId, ApplicationDate,
            [Status], ResumeSummary, ExperienceYears, ContactPhone
        )
        VALUES (
            @JobPostingId, @EmployeeId, GETUTCDATE(),
            0, -- 0: Pending
            @ResumeSummary, @ExperienceYears, @ContactPhone
        );

        SET @ApplicationId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

-- =============================================
-- Stored Procedure: usp_CreateFair
-- Description: Creates a new cultural fair, populates operating days, and generates stalls
-- =============================================
CREATE OR ALTER PROCEDURE usp_CreateFair
    @Title NVARCHAR(200),
    @Description NVARCHAR(MAX),
    @Location NVARCHAR(200),
    @StartDate DATE,
    @EndDate DATE,
    @DailyCapacity INT,
    @TotalStalls INT,
    @BaseStallPrice DECIMAL(18, 2),
    @BaseTicketPrice DECIMAL(18, 2),
    @BannerImageUrl NVARCHAR(500),
    @FairId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Insert Fair record
        INSERT INTO Fairs (
            Title, Description, Location, StartDate, EndDate,
            DailyCapacity, TotalStalls, AvailableStalls,
            BaseStallPrice, BaseTicketPrice, BannerImageUrl,
            IsActive, CreatedAt
        )
        VALUES (
            @Title, @Description, @Location, @StartDate, @EndDate,
            @DailyCapacity, @TotalStalls, @TotalStalls,
            @BaseStallPrice, @BaseTicketPrice, ISNULL(@BannerImageUrl, ''),
            1, GETUTCDATE()
        );

        SET @FairId = SCOPE_IDENTITY();

        -- 2. Generate FairDays between StartDate and EndDate
        DECLARE @CurrentDate DATE = @StartDate;
        WHILE @CurrentDate <= @EndDate
        BEGIN
            INSERT INTO FairDays (FairId, [Date], DailyCapacity, TicketsSold, IsActive)
            VALUES (@FairId, @CurrentDate, @DailyCapacity, 0, 1);

            SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
        END;

        -- 3. Generate Stalls with diverse categories and standard dimensions
        DECLARE @StallIndex INT = 1;
        DECLARE @StallNum NVARCHAR(50);
        DECLARE @Category INT;
        DECLARE @Size INT;
        DECLARE @Price DECIMAL(18, 2);

        WHILE @StallIndex <= @TotalStalls
        BEGIN
            SET @StallNum = 'S-' + RIGHT('000' + CAST(@StallIndex AS NVARCHAR(10)), 3);
            
            -- Distribute categories across stalls
            SET @Category = (@StallIndex % 7);
            
            -- Assign sizes (Corner/Large for every 5th stall)
            IF (@StallIndex % 5 = 0)
            BEGIN
                SET @Size = 3; -- PremiumCorner
                SET @Price = @BaseStallPrice * 1.5;
            END
            ELSE IF (@StallIndex % 3 = 0)
            BEGIN
                SET @Size = 2; -- Large
                SET @Price = @BaseStallPrice * 1.25;
            END
            ELSE IF (@StallIndex % 2 = 0)
            BEGIN
                SET @Size = 1; -- Medium
                SET @Price = @BaseStallPrice;
            END
            ELSE
            BEGIN
                SET @Size = 0; -- Small
                SET @Price = @BaseStallPrice * 0.85;
            END;

            INSERT INTO Stalls (FairId, StallNumber, Category, [Size], Price, IsBooked)
            VALUES (@FairId, @StallNum, @Category, @Size, @Price, 0);

            SET @StallIndex = @StallIndex + 1;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

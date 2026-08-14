-- =============================================
-- Stored Procedure: usp_BuyFairTicket
-- Description: Concurrency-safe group ticket purchasing with row-level capacity locking
-- =============================================
CREATE OR ALTER PROCEDURE usp_BuyFairTicket
    @FairId INT,
    @FairDayId INT,
    @VisitorId NVARCHAR(450),
    @Quantity INT,
    @TotalPaid DECIMAL(18, 2) OUTPUT,
    @TicketCode NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate quantity
        IF @Quantity <= 0
        BEGIN
            THROW 50006, 'Ticket quantity must be at least 1.', 1;
        END;

        -- 1. Row-level lock on FairDay capacity
        DECLARE @DailyCapacity INT;
        DECLARE @TicketsSold INT;

        SELECT 
            @DailyCapacity = DailyCapacity,
            @TicketsSold = TicketsSold
        FROM FairDays WITH (UPDLOCK, ROWLOCK)
        WHERE FairDayId = @FairDayId AND FairId = @FairId AND IsActive = 1;

        IF @DailyCapacity IS NULL
        BEGIN
            THROW 50004, 'The requested fair day schedule was not found or is inactive.', 1;
        END;

        -- 2. Validate Admission Capacity
        IF (@TicketsSold + @Quantity) > @DailyCapacity
        BEGIN
            DECLARE @Remaining INT = @DailyCapacity - @TicketsSold;
            DECLARE @ErrMsg NVARCHAR(250) = CONCAT('Admission capacity exceeded! Only ', @Remaining, ' ticket(s) remaining for this date.');
            THROW 50002, @ErrMsg, 1;
        END;

        -- 3. Retrieve Base Ticket Price
        DECLARE @BaseTicketPrice DECIMAL(18, 2);
        SELECT @BaseTicketPrice = BaseTicketPrice
        FROM Fairs
        WHERE FairId = @FairId;

        SET @TotalPaid = @BaseTicketPrice * @Quantity;

        -- 4. Update TicketsSold count on FairDay
        UPDATE FairDays
        SET TicketsSold = TicketsSold + @Quantity
        WHERE FairDayId = @FairDayId;

        -- 5. Generate Unique Ticket Code & Save Ticket Record
        SET @TicketCode = 'TKT-' + UPPER(SUBSTRING(CONVERT(NVARCHAR(36), NEWID()), 1, 8)) + '-' + CAST(@FairId AS NVARCHAR(10));

        INSERT INTO Tickets (
            FairId, FairDayId, VisitorId, TicketCode,
            PurchaseDate, PricePaid, Quantity, [Status]
        )
        VALUES (
            @FairId, @FairDayId, @VisitorId, @TicketCode,
            GETUTCDATE(), @TotalPaid, @Quantity, 0 -- 0: Valid
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

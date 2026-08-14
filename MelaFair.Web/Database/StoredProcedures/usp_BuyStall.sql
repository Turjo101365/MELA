-- =============================================
-- Stored Procedure: usp_BuyStall
-- Description: High-concurrency safe Vendor stall booking using UPDLOCK and ROWLOCK
-- =============================================
CREATE OR ALTER PROCEDURE usp_BuyStall
    @FairId INT,
    @StallId INT,
    @VendorId NVARCHAR(450),
    @Notes NVARCHAR(500) = NULL,
    @BookingId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Check stall existence and acquire row-level exclusive update lock
        DECLARE @CurrentIsBooked BIT;
        DECLARE @StallPrice DECIMAL(18,2);

        SELECT 
            @CurrentIsBooked = IsBooked,
            @StallPrice = Price
        FROM Stalls WITH (UPDLOCK, ROWLOCK)
        WHERE StallId = @StallId AND FairId = @FairId;

        -- Validate existence
        IF @StallPrice IS NULL
        BEGIN
            THROW 50004, 'The requested stall or fair was not found.', 1;
        END;

        -- 2. Concurrency Safety: Check if already leased
        IF @CurrentIsBooked = 1
        BEGIN
            THROW 50001, 'This stall has already been booked by another vendor. Please select a different stall.', 1;
        END;

        -- 3. Update stall status to booked
        UPDATE Stalls
        SET IsBooked = 1
        WHERE StallId = @StallId;

        -- 4. Generate unique transaction reference
        DECLARE @TxnRef NVARCHAR(100) = 'STALL-TXN-' + UPPER(SUBSTRING(CONVERT(NVARCHAR(36), NEWID()), 1, 8));

        -- 5. Insert Booking Record
        INSERT INTO StallBookings (
            StallId, FairId, VendorId, BookingDate,
            AmountPaid, PaymentStatus, TransactionReference, Notes
        )
        VALUES (
            @StallId, @FairId, @VendorId, GETUTCDATE(),
            @StallPrice, 1, @TxnRef, ISNULL(@Notes, '')
        );

        SET @BookingId = SCOPE_IDENTITY();

        -- 6. Decrement available stalls on the Fair record
        UPDATE Fairs
        SET AvailableStalls = (SELECT COUNT(*) FROM Stalls WHERE FairId = @FairId AND IsBooked = 0)
        WHERE FairId = @FairId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

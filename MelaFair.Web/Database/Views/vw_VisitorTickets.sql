-- =============================================
-- View: vw_VisitorTickets
-- Description: Detailed view of issued visitor tickets with fair and visitor information
-- =============================================
CREATE OR ALTER VIEW vw_VisitorTickets
AS
SELECT 
    t.TicketId,
    t.TicketCode,
    t.FairId,
    f.Title AS FairTitle,
    f.Location,
    fd.[Date] AS VisitDate,
    t.VisitorId,
    u.FullName AS VisitorName,
    u.Email AS VisitorEmail,
    t.Quantity,
    t.PricePaid,
    t.[Status],
    t.PurchaseDate
FROM Tickets t
JOIN Fairs f ON t.FairId = f.FairId
JOIN FairDays fd ON t.FairDayId = fd.FairDayId
JOIN AspNetUsers u ON t.VisitorId = u.Id;

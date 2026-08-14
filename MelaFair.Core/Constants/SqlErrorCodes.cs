namespace MelaFair.Core.Constants;

/// <summary>
/// Custom error numbers raised by SQL Server stored procedures and triggers
/// </summary>
public static class SqlErrorCodes
{
    public const int StallAlreadyBooked = 50001;
    public const int DailyCapacityExceeded = 50002;
    public const int DuplicateJobApplication = 50003;
    public const int FairInactiveOrNotFound = 50004;
    public const int JobPositionsFilled = 50005;
}
